using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using Quizory.Api.Data;
using Quizory.Api.Domain;
using Quizory.Api.Dtos;
using Quizory.Api.Services;

namespace Quizory.Api.Services;

public interface IExportImportService
{
    Task<byte[]> GetImportTemplateAsync(string templateVersion = "1.0");
    Task<byte[]> ExportQuizToExcelAsync(Guid orgId, Guid quizId);
    Task<byte[]> ExportQuizToPdfAsync(Guid orgId, Guid quizId);
    Task<byte[]> ExportLeagueToExcelAsync(Guid orgId, Guid leagueId);
    Task<(bool Success, string? Error)> ImportFromExcelAsync(Guid orgId, Stream stream, string templateVersion);
}

public class ExportImportService(AppDbContext db, IScoringService scoring) : IExportImportService
{
    public Task<byte[]> GetImportTemplateAsync(string templateVersion = "1.0")
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Import");
        ws.Cell(1, 1).Value = templateVersion;
        ws.Cell(1, 2).Value = "Quiz Name";
        ws.Cell(2, 1).Value = "Date";
        ws.Cell(2, 2).Value = DateTime.UtcNow.ToString("yyyy-MM-dd");
        ws.Cell(4, 1).Value = "Team";
        ws.Cell(4, 2).Value = "Category1";
        ws.Cell(4, 3).Value = "Category2";
        ws.Cell(5, 1).Value = "Team A";
        ws.Cell(5, 2).Value = 0;
        ws.Cell(5, 3).Value = 0;
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }

    public async Task<byte[]> ExportQuizToExcelAsync(Guid orgId, Guid quizId)
    {
        var quiz = await db.Quizzes.FindAsync(quizId);
        if (quiz == null || quiz.OrganizationId != orgId) throw new InvalidOperationException("Quiz not found.");
        var categoryList = await db.QuizCategories.Where(qc => qc.QuizId == quizId).OrderBy(qc => qc.OrderIndex)
            .Join(db.Categories.IgnoreQueryFilters(), qc => qc.CategoryId, c => c.Id, (qc, c) => new { qc.CategoryId, c.Name }).ToListAsync();
        if (categoryList.Count == 0)
            categoryList = await db.ScoreEntries.Where(s => s.QuizId == quizId).Select(s => s.CategoryId).Distinct()
                .Join(db.Categories.IgnoreQueryFilters(), id => id, c => c.Id, (id, c) => new { CategoryId = id, c.Name }).ToListAsync();
        var categoryIdsOrdered = categoryList.Select(x => x.CategoryId).ToList();
        var categoryNames = categoryList.Select(x => x.Name).ToList();
        var teamIds = await db.QuizTeams.Where(qt => qt.QuizId == quizId).Select(qt => qt.TeamId).ToListAsync();
        var teams = await db.Teams.IgnoreQueryFilters().Where(t => teamIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id);
        var quizTeams = await db.QuizTeams.Where(qt => qt.QuizId == quizId).ToDictionaryAsync(qt => qt.TeamId, qt => qt.AliasInQuiz);
        var ranks = await scoring.ComputeRankingAsync(quizId, orgId);
        var orderedTeams = ranks.OrderByDescending(x => x.Value).Select(x => x.Key).ToList();
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(quiz.Name.Length > 31 ? quiz.Name[..31] : quiz.Name);
        ws.Cell(1, 1).Value = "1.0";
        ws.Cell(1, 2).Value = quiz.Name;
        ws.Cell(2, 1).Value = "Date";
        ws.Cell(2, 2).Value = quiz.DateUtc.ToString("yyyy-MM-dd");
        int col = 1;
        ws.Cell(4, col).Value = "Team";
        col++;
        foreach (var cat in categoryNames)
        {
            ws.Cell(4, col).Value = cat;
            col++;
        }
        ws.Cell(4, col).Value = "Total";
        ws.Cell(4, col + 1).Value = "Rank";
        int row = 5;
        foreach (var teamId in orderedTeams)
        {
            col = 1;
            var displayName = quizTeams.GetValueOrDefault(teamId) ?? teams.GetValueOrDefault(teamId)?.Name ?? teamId.ToString();
            ws.Cell(row, col).Value = displayName;
            col++;
            foreach (var catId in categoryIdsOrdered)
            {
                var entry = await db.ScoreEntries.FirstOrDefaultAsync(s => s.QuizId == quizId && s.TeamId == teamId && s.CategoryId == catId);
                var pts = (entry?.Points ?? 0) + (entry?.BonusPoints ?? 0);
                ws.Cell(row, col).Value = pts;
                col++;
            }
            ws.Cell(row, col).Value = ranks.GetValueOrDefault(teamId, 0);
            ws.Cell(row, col + 1).Value = row - 4;
            row++;
        }
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportQuizToPdfAsync(Guid orgId, Guid quizId)
    {
        var quiz = await db.Quizzes.FindAsync(quizId);
        if (quiz == null || quiz.OrganizationId != orgId) throw new InvalidOperationException("Quiz not found.");
        var ranks = await scoring.ComputeRankingAsync(quizId, orgId);
        var ordered = ranks.OrderByDescending(x => x.Value).ToList();
        var teamIds = ordered.Select(x => x.Key).ToList();
        var teams = await db.Teams.IgnoreQueryFilters().Where(t => teamIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id);
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.Header().Text(quiz.Name).Bold().FontSize(18);
                page.Content().Column(column =>
                {
                    column.Item().Text($"Date: {quiz.DateUtc:yyyy-MM-dd}").FontSize(10);
                    column.Item().PaddingVertical(10);
                    int r = 1;
                    foreach (var (teamId, points) in ordered)
                    {
                        var name = teams.GetValueOrDefault(teamId)?.Name ?? teamId.ToString();
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"{r}. {name}");
                            row.RelativeItem().AlignRight().Text(points.ToString());
                        });
                        r++;
                    }
                });
            });
        });
        using var ms = new MemoryStream();
        doc.GeneratePdf(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportLeagueToExcelAsync(Guid orgId, Guid leagueId)
    {
        var league = await db.Leagues.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == leagueId && l.OrganizationId == orgId && !l.IsDeleted);
        if (league == null) throw new InvalidOperationException("League not found.");
        var quizIds = await db.Quizzes.Where(q => q.OrganizationId == orgId && q.LeagueId == leagueId).Select(q => q.Id).ToListAsync();
        var teamTotals = new Dictionary<Guid, int>();
        foreach (var qid in quizIds)
        {
            var ranks = await scoring.ComputeRankingAsync(qid, orgId);
            foreach (var (tid, pts) in ranks)
                teamTotals[tid] = teamTotals.GetValueOrDefault(tid, 0) + pts;
        }
        var ordered = teamTotals.OrderByDescending(x => x.Value).ToList();
        var teams = await db.Teams.IgnoreQueryFilters().Where(t => ordered.Select(x => x.Key).Contains(t.Id)).ToDictionaryAsync(t => t.Id);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(league.Name.Length > 31 ? league.Name[..31] : league.Name);
        ws.Cell(1, 1).Value = league.Name;
        ws.Cell(2, 1).Value = "League standings";
        ws.Cell(4, 1).Value = "Rank";
        ws.Cell(4, 2).Value = "Team";
        ws.Cell(4, 3).Value = "Total points";
        int row = 5;
        for (int i = 0; i < ordered.Count; i++)
        {
            var (teamId, points) = ordered[i];
            ws.Cell(row, 1).Value = i + 1;
            ws.Cell(row, 2).Value = teams.GetValueOrDefault(teamId)?.Name ?? teamId.ToString();
            ws.Cell(row, 3).Value = points;
            row++;
        }
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<(bool Success, string? Error)> ImportFromExcelAsync(Guid orgId, Stream stream, string templateVersion)
    {
        try
        {
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheet(1);
            var templateVer = ws.Cell("A1").GetString();
            if (templateVer != templateVersion)
                return (false, "Invalid template version.");
            var quizName = ws.Cell("B1").GetString();
            var dateStr = ws.Cell("B2").GetString();
            if (!DateTime.TryParse(dateStr, out var date))
                return (false, "Invalid date.");
            var quiz = new Quiz { OrganizationId = orgId, Name = quizName, DateUtc = date, Location = "", Status = QuizStatus.Finished };
            db.Quizzes.Add(quiz);
            var headerRow = 4;
            var cols = new List<string>();
            var c = 1;
            while (true)
            {
                var val = ws.Cell(headerRow, c).GetString();
                if (string.IsNullOrEmpty(val)) break;
                cols.Add(val);
                c++;
            }
            var categoryNames = cols.Skip(1).Take(cols.Count - 2).ToList();
            var categoryIds = new List<Guid>();
            foreach (var name in categoryNames)
            {
                var cat = await db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.OrganizationId == orgId && c.Name == name && !c.IsDeleted);
                if (cat == null)
                {
                    cat = new Category { OrganizationId = orgId, Name = name };
                    db.Categories.Add(cat);
                    await db.SaveChangesAsync();
                }
                categoryIds.Add(cat.Id);
            }
            for (int i = 0; i < categoryIds.Count; i++)
                db.QuizCategories.Add(new QuizCategory { QuizId = quiz.Id, CategoryId = categoryIds[i], OrderIndex = i });
            var row = 5;
            while (true)
            {
                var teamName = ws.Cell(row, 1).GetString();
                if (string.IsNullOrEmpty(teamName)) break;
                var team = await db.Teams.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.OrganizationId == orgId && t.Name == teamName && !t.IsDeleted);
                if (team == null)
                {
                    team = new Team { OrganizationId = orgId, Name = teamName };
                    db.Teams.Add(team);
                    await db.SaveChangesAsync();
                }
                db.QuizTeams.Add(new QuizTeam { QuizId = quiz.Id, TeamId = team.Id, OrganizationId = orgId });
                for (int i = 0; i < categoryIds.Count; i++)
                {
                    var pts = ws.Cell(row, i + 2).TryGetValue(out int v) ? v : 0;
                    db.ScoreEntries.Add(new ScoreEntry
                    {
                        QuizId = quiz.Id,
                        TeamId = team.Id,
                        CategoryId = categoryIds[i],
                        OrganizationId = orgId,
                        Points = pts,
                        BonusPoints = 0
                    });
                }
                row++;
            }
            await db.SaveChangesAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
