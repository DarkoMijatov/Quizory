using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Quizory.Api.Data;
using Quizory.Api.Domain;
using Quizory.Api.Dtos;
using Quizory.Api.Services;

namespace Quizory.Api.Services;

public interface IShareService
{
    Task<ShareTokenResponse?> CreateTokenAsync(Guid quizId, DateTime? expiresAtUtc);
    Task<ShareLeaderboardDto?> GetByTokenAsync(string token);
}

public record ShareTokenResponse(string Token, string Url, DateTime? ExpiresAtUtc);

public class ShareService(AppDbContext db, IScoringService scoring) : IShareService
{
    public async Task<ShareTokenResponse?> CreateTokenAsync(Guid quizId, DateTime? expiresAtUtc)
    {
        var quiz = await db.Quizzes.FindAsync(quizId);
        if (quiz == null) return null;
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        db.PublicShareTokens.Add(new PublicShareToken
        {
            OrganizationId = quiz.OrganizationId,
            QuizId = quizId,
            Token = token,
            ExpiresAtUtc = expiresAtUtc
        });
        await db.SaveChangesAsync();
        return new ShareTokenResponse(token, $"/api/share/leaderboard/{token}", expiresAtUtc);
    }

    public async Task<ShareLeaderboardDto?> GetByTokenAsync(string token)
    {
        var record = await db.PublicShareTokens.FirstOrDefaultAsync(t => t.Token == token);
        if (record == null) return null;
        if (record.ExpiresAtUtc.HasValue && record.ExpiresAtUtc.Value < DateTime.UtcNow) return null;
        var quiz = await db.Quizzes.FindAsync(record.QuizId);
        if (quiz == null) return null;
        var org = await db.Organizations.FindAsync(record.OrganizationId);
        var ranks = await scoring.ComputeRankingAsync(record.QuizId, record.OrganizationId);
        var ordered = ranks.OrderByDescending(x => x.Value).ToList();
        var teamIds = ordered.Select(x => x.Key).ToList();
        var teams = await db.Teams.IgnoreQueryFilters().Where(t => teamIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id);
        var rankings = ordered.Select((x, i) => new TeamRankDto(x.Key, teams.GetValueOrDefault(x.Key)?.Name ?? "", i + 1, x.Value)).ToList();
        return new ShareLeaderboardDto(rankings, quiz.Name, quiz.DateUtc, org?.PrimaryColor);
    }
}
