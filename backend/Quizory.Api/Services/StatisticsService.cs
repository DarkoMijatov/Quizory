using Microsoft.EntityFrameworkCore;
using Quizory.Api.Data;
using Quizory.Api.Domain;
using Quizory.Api.Dtos;

namespace Quizory.Api.Services;

public interface IStatisticsService
{
    Task<(List<QuizSummaryDto> Items, int Total)> GetQuizSummariesAsync(Guid orgId, DateTime? from, DateTime? to, Guid? leagueId, Guid? teamId, int page, int pageSize);
    Task<LeagueSummaryDto?> GetLeagueSummaryAsync(Guid orgId, Guid leagueId);
    Task<List<CategoryStatsDto>> GetCategoryStatsAsync(Guid orgId, DateTime? from, DateTime? to, Guid? leagueId);
    Task<List<TeamRankDto>> GetTeamHistoryAsync(Guid orgId, Guid teamId, Guid? leagueId, int limit);
}

public class StatisticsService(AppDbContext db, IScoringService scoring) : IStatisticsService
{
    public async Task<(List<QuizSummaryDto> Items, int Total)> GetQuizSummariesAsync(Guid orgId, DateTime? from, DateTime? to, Guid? leagueId, Guid? teamId, int page, int pageSize)
    {
        var query = db.Quizzes.Where(q => q.OrganizationId == orgId);
        if (from.HasValue) query = query.Where(q => q.DateUtc >= from);
        if (to.HasValue) query = query.Where(q => q.DateUtc <= to);
        if (leagueId.HasValue) query = query.Where(q => q.LeagueId == leagueId);
        if (teamId.HasValue)
            query = query.Where(q => db.QuizTeams.Any(qt => qt.QuizId == q.Id && qt.TeamId == teamId));

        var total = await query.CountAsync();
        var quizzes = await query.OrderByDescending(q => q.DateUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var result = new List<QuizSummaryDto>();
        foreach (var q in quizzes)
        {
            var ranks = await scoring.ComputeRankingAsync(q.Id, orgId);
            var ordered = ranks.OrderByDescending(x => x.Value).ToList();
            var winner = ordered.FirstOrDefault();
            var teamCount = await db.QuizTeams.CountAsync(qt => qt.QuizId == q.Id);
            var categoryCount = await db.QuizCategories.CountAsync(qc => qc.QuizId == q.Id);
            if (categoryCount == 0) categoryCount = await db.ScoreEntries.Where(s => s.QuizId == q.Id).Select(s => s.CategoryId).Distinct().CountAsync();
            result.Add(new QuizSummaryDto(
                q.Id, q.Name, q.DateUtc, q.Location, q.Status.ToString(), teamCount, categoryCount,
                winner.Key == Guid.Empty ? null : winner.Key, winner.Value));
        }
        return (result, total);
    }

    public async Task<LeagueSummaryDto?> GetLeagueSummaryAsync(Guid orgId, Guid leagueId)
    {
        var league = await db.Leagues.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == leagueId && l.OrganizationId == orgId && !l.IsDeleted);
        if (league == null) return null;
        var quizIds = await db.Quizzes.Where(q => q.OrganizationId == orgId && q.LeagueId == leagueId).Select(q => q.Id).ToListAsync();
        var teamTotals = new Dictionary<Guid, int>();
        foreach (var quizId in quizIds)
        {
            var ranks = await scoring.ComputeRankingAsync(quizId, orgId);
            foreach (var (teamId, points) in ranks)
            {
                teamTotals[teamId] = teamTotals.GetValueOrDefault(teamId, 0) + points;
            }
        }
        var ordered = teamTotals.OrderByDescending(x => x.Value).Take(20).ToList();
        var teamIds = ordered.Select(x => x.Key).ToList();
        var teams = await db.Teams.IgnoreQueryFilters().Where(t => teamIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id);
        var topTeams = ordered.Select((x, i) => new TeamRankDto(x.Key, teams.GetValueOrDefault(x.Key)?.Name ?? "", i + 1, x.Value)).ToList();
        return new LeagueSummaryDto(leagueId, league.Name, quizIds.Count, topTeams);
    }

    public async Task<List<CategoryStatsDto>> GetCategoryStatsAsync(Guid orgId, DateTime? from, DateTime? to, Guid? leagueId)
    {
        var quizQuery = db.Quizzes.Where(q => q.OrganizationId == orgId);
        if (from.HasValue) quizQuery = quizQuery.Where(q => q.DateUtc >= from);
        if (to.HasValue) quizQuery = quizQuery.Where(q => q.DateUtc <= to);
        if (leagueId.HasValue) quizQuery = quizQuery.Where(q => q.LeagueId == leagueId);
        var quizIds = await quizQuery.Select(q => q.Id).ToListAsync();
        var entries = await db.ScoreEntries.Where(s => s.OrganizationId == orgId && quizIds.Contains(s.QuizId))
            .GroupBy(s => new { s.CategoryId, s.QuizId })
            .Select(g => new { g.Key.CategoryId, g.Key.QuizId, Total = g.Sum(x => x.Points + x.BonusPoints) })
            .ToListAsync();
        var jokerTeamsByQuiz = await db.HelpUsages.Where(u => u.OrganizationId == orgId && quizIds.Contains(u.QuizId))
            .Join(db.HelpTypes, u => u.HelpTypeId, h => h.Id, (u, h) => new { u.QuizId, u.TeamId, h.Behavior })
            .Where(x => x.Behavior == HelpBehavior.DoubleScore)
            .ToListAsync();
        var categoryIds = entries.Select(x => x.CategoryId).Distinct().ToList();
        var categories = await db.Categories.IgnoreQueryFilters().Where(c => categoryIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id);
        return entries.GroupBy(x => x.CategoryId).Select(g =>
        {
            var catId = g.Key;
            var pointsList = g.Select(x => (double)x.Total).ToList();
            var avg = pointsList.Count > 0 ? pointsList.Average() : 0;
            return new CategoryStatsDto(catId, categories.GetValueOrDefault(catId)?.Name ?? "", avg, g.Select(x => x.QuizId).Distinct().Count());
        }).ToList();
    }

    public async Task<List<TeamRankDto>> GetTeamHistoryAsync(Guid orgId, Guid teamId, Guid? leagueId, int limit)
    {
        var query = db.Quizzes.Where(q => q.OrganizationId == orgId && db.QuizTeams.Any(qt => qt.QuizId == q.Id && qt.TeamId == teamId));
        if (leagueId.HasValue) query = query.Where(q => q.LeagueId == leagueId);
        var quizzes = await query.OrderByDescending(q => q.DateUtc).Take(limit).ToListAsync();
        var team = await db.Teams.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == teamId);
        var results = new List<TeamRankDto>();
        foreach (var q in quizzes)
        {
            var ranks = await scoring.ComputeRankingAsync(q.Id, orgId);
            var ordered = ranks.OrderByDescending(x => x.Value).ToList();
            var rank = ordered.FindIndex(x => x.Key == teamId) + 1;
            if (rank == 0) continue;
            var points = ranks.GetValueOrDefault(teamId, 0);
            results.Add(new TeamRankDto(teamId, team?.Name ?? "", rank, points));
        }
        return results;
    }
}