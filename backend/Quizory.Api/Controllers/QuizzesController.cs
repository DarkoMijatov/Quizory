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
    ITextLocalizer localizer) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var ctx = context.Get();
        var quizzes = await db.Quizzes.Where(x => x.OrganizationId == ctx.OrganizationId && !x.IsDeleted).ToListAsync();
        return Ok(quizzes);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQuizRequest request)
    {
        var ctx = context.Get();
        subscriptions.EnforceQuizMonthlyLimit();

        var quiz = new Quiz
        {
            Name = request.Name,
            DateUtc = request.DateUtc,
            Location = request.Location,
            LeagueId = request.LeagueId,
            OrganizationId = ctx.OrganizationId
        };
        db.Quizzes.Add(quiz);

        foreach (var teamId in request.TeamIds)
        {
            db.QuizTeams.Add(new QuizTeam { QuizId = quiz.Id, TeamId = teamId, OrganizationId = ctx.OrganizationId });
            foreach (var categoryId in request.CategoryIds)
            {
                db.ScoreEntries.Add(new ScoreEntry { QuizId = quiz.Id, TeamId = teamId, CategoryId = categoryId, OrganizationId = ctx.OrganizationId });
            }
        }

        await db.SaveChangesAsync();
        return Ok(new { message = localizer.T("QuizCreated"), quiz });
    }

    [HttpPost("{quizId:guid}/scores")]
    public async Task<IActionResult> UpsertScore(Guid quizId, [FromBody] UpdateScoreRequest request)
    {
        var ctx = context.Get();
        var score = await db.ScoreEntries.FirstOrDefaultAsync(x => x.QuizId == quizId && x.TeamId == request.TeamId && x.CategoryId == request.CategoryId && x.OrganizationId == ctx.OrganizationId);
        if (score is null) return NotFound();
        if (score.IsLocked) return BadRequest("Category score is locked.");

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
        var ctx = context.Get();
        var exists = await db.HelpUsages.AnyAsync(x => x.OrganizationId == ctx.OrganizationId && x.QuizId == quizId && x.TeamId == request.TeamId && x.HelpTypeId == request.HelpTypeId);
        if (exists) return BadRequest("Help already used for this team in this quiz.");

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
        var ctx = context.Get();
        var ranks = await scoring.ComputeRankingAsync(quizId, ctx.OrganizationId);
        var ordered = ranks.OrderByDescending(x => x.Value).Select((x, index) => new { rank = index + 1, teamId = x.Key, points = x.Value });
        return Ok(ordered);
    }
}
