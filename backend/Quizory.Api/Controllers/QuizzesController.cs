using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizory.Api.Data;
using Quizory.Api.Domain;
using Quizory.Api.Dtos;
using Quizory.Api.Services;

namespace Quizory.Api.Controllers;

[ApiController]
[Route("api/quizzes")]
public class QuizzesController(
    AppDbContext db,
    IRequestContextAccessor context,
    ISubscriptionService subscriptions,
    IScoringService scoring,
    ITextLocalizer localizer,
    IOrgAuthorizationService auth) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? leagueId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        var ctx = context.Get();
        var query = db.Quizzes.Where(x => x.OrganizationId == ctx.OrganizationId);
        if (leagueId.HasValue) query = query.Where(q => q.LeagueId == leagueId);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<QuizStatus>(status, true, out var s))
            query = query.Where(q => q.Status == s);
        var total = await query.CountAsync();
        var quizzes = await query.OrderByDescending(q => q.DateUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PaginatedResponse<Quiz>(quizzes, total, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        var ctx = context.Get();
        var quiz = await db.Quizzes.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == ctx.OrganizationId);
        if (quiz == null) return NotFound();
        var categories = await db.QuizCategories.Where(qc => qc.QuizId == id).OrderBy(qc => qc.OrderIndex)
            .Join(db.Categories.IgnoreQueryFilters(), qc => qc.CategoryId, c => c.Id, (qc, c) => c).ToListAsync();
        if (categories.Count == 0)
            categories = await db.ScoreEntries.Where(se => se.QuizId == id).Select(se => se.CategoryId).Distinct()
                .Join(db.Categories.IgnoreQueryFilters(), id => id, c => c.Id, (_, c) => c).ToListAsync();
        var teams = await db.QuizTeams.Where(qt => qt.QuizId == id).ToListAsync();
        var teamIds = teams.Select(qt => qt.TeamId).ToList();
        var teamNames = await db.Teams.IgnoreQueryFilters().Where(t => teamIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name);
        return Ok(new { quiz, categories, teams = teams.Select(qt => new { qt.TeamId, qt.AliasInQuiz, Name = qt.AliasInQuiz ?? teamNames.GetValueOrDefault(qt.TeamId) }) });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQuizRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        var ctx = context.Get();
        if (request.LeagueId.HasValue) subscriptions.EnforceFeature("leagues");
        subscriptions.EnforceQuizMonthlyLimit();

        var quiz = new Quiz
        {
            Name = request.Name,
            DateUtc = request.DateUtc,
            Location = request.Location,
            LeagueId = request.LeagueId,
            OrganizationId = ctx.OrganizationId,
            OverrideCategoriesCount = request.CategoryIds.Count,
            OverrideQuestionsPerCategory = null
        };
        db.Quizzes.Add(quiz);

        for (int i = 0; i < request.CategoryIds.Count; i++)
            db.QuizCategories.Add(new QuizCategory { QuizId = quiz.Id, CategoryId = request.CategoryIds[i], OrderIndex = i });

        foreach (var teamId in request.TeamIds)
        {
            var aliasInQuiz = request.TeamAliasesInQuiz != null && request.TeamAliasesInQuiz.TryGetValue(teamId, out var a) ? a : null;
            db.QuizTeams.Add(new QuizTeam { QuizId = quiz.Id, TeamId = teamId, OrganizationId = ctx.OrganizationId, AliasInQuiz = aliasInQuiz });
            foreach (var categoryId in request.CategoryIds)
            {
                db.ScoreEntries.Add(new ScoreEntry { QuizId = quiz.Id, TeamId = teamId, CategoryId = categoryId, OrganizationId = ctx.OrganizationId });
            }
        }

        await db.SaveChangesAsync();
        return Ok(new { message = localizer.T("QuizCreated"), quiz });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateQuizRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        var ctx = context.Get();
        var quiz = await db.Quizzes.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == ctx.OrganizationId);
        if (quiz == null) return NotFound();
        if (request.Name != null) quiz.Name = request.Name;
        if (request.DateUtc.HasValue) quiz.DateUtc = request.DateUtc.Value;
        if (request.Location != null) quiz.Location = request.Location;
        if (request.LeagueId.HasValue) subscriptions.EnforceFeature("leagues");
        if (request.LeagueId != null) quiz.LeagueId = request.LeagueId;
        if (request.Status.HasValue) quiz.Status = request.Status.Value;
        if (request.OverrideCategoriesCount.HasValue) quiz.OverrideCategoriesCount = request.OverrideCategoriesCount;
        if (request.OverrideQuestionsPerCategory.HasValue) quiz.OverrideQuestionsPerCategory = request.OverrideQuestionsPerCategory;
        await db.SaveChangesAsync();
        return Ok(quiz);
    }

    [HttpPost("{quizId:guid}/finish")]
    public async Task<IActionResult> Finish(Guid quizId)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        var ctx = context.Get();
        var quiz = await db.Quizzes.FirstOrDefaultAsync(x => x.Id == quizId && x.OrganizationId == ctx.OrganizationId);
        if (quiz == null) return NotFound();
        quiz.Status = QuizStatus.Finished;
        await db.SaveChangesAsync();
        return Ok(quiz);
    }

    [HttpPost("{quizId:guid}/scores")]
    public async Task<IActionResult> UpsertScore(Guid quizId, [FromBody] UpdateScoreRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        var ctx = context.Get();
        var score = await db.ScoreEntries.FirstOrDefaultAsync(x => x.QuizId == quizId && x.TeamId == request.TeamId && x.CategoryId == request.CategoryId && x.OrganizationId == ctx.OrganizationId);
        if (score is null) return NotFound();
        if (score.IsLocked) return BadRequest(new { errorCode = "CategoryLocked", message = localizer.T("CategoryLocked") });

        score.Points = request.Points;
        score.BonusPoints = request.BonusPoints;
        score.Notes = request.Notes;
        score.IsLocked = request.IsLocked;
        await db.SaveChangesAsync();
        return Ok(score);
    }

    [HttpPost("{quizId:guid}/helps")]
    public async Task<IActionResult> ApplyHelp(Guid quizId, [FromBody] ApplyHelpRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        var ctx = context.Get();
        var exists = await db.HelpUsages.AnyAsync(x => x.OrganizationId == ctx.OrganizationId && x.QuizId == quizId && x.TeamId == request.TeamId && x.HelpTypeId == request.HelpTypeId);
        if (exists) return BadRequest(new { errorCode = "HelpAlreadyUsed", message = localizer.T("HelpAlreadyUsed") });

        db.HelpUsages.Add(new HelpUsage
        {
            OrganizationId = ctx.OrganizationId,
            QuizId = quizId,
            TeamId = request.TeamId,
            HelpTypeId = request.HelpTypeId
        });
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{quizId:guid}/ranking")]
    public async Task<IActionResult> Ranking(Guid quizId)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        var ctx = context.Get();
        var ranks = await scoring.ComputeRankingAsync(quizId, ctx.OrganizationId);
        var teamIds = ranks.Keys.ToList();
        var teams = await db.Teams.IgnoreQueryFilters().Where(t => teamIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id);
        var quizTeams = await db.QuizTeams.Where(qt => qt.QuizId == quizId).ToDictionaryAsync(qt => qt.TeamId, qt => qt.AliasInQuiz);
        var ordered = ranks.OrderByDescending(x => x.Value).Select((x, index) => new
        {
            rank = index + 1,
            teamId = x.Key,
            teamName = quizTeams.GetValueOrDefault(x.Key) ?? teams.GetValueOrDefault(x.Key)?.Name ?? x.Key.ToString(),
            points = x.Value
        });
        return Ok(ordered);
    }
}
