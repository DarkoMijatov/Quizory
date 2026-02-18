using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizory.Api.Data;
using Quizory.Api.Domain;
using Quizory.Api.Dtos;
using Quizory.Api.Services;

namespace Quizory.Api.Controllers;

[ApiController]
[Route("api/questions")]
public class QuestionsController(AppDbContext db, IRequestContextAccessor context, IOrgAuthorizationService auth, ISubscriptionService subscriptions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? categoryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        subscriptions.EnforceFeature("questionBank");
        var ctx = context.Get();
        var query = db.Questions.Where(q => q.OrganizationId == ctx.OrganizationId);
        if (categoryId.HasValue) query = query.Where(q => q.CategoryId == categoryId.Value);
        var total = await query.CountAsync();
        var items = await query.OrderBy(q => q.OrderIndex).ThenBy(q => q.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PaginatedResponse<Question>(items, total, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        subscriptions.EnforceFeature("questionBank");
        var ctx = context.Get();
        var question = await db.Questions.Include(q => q.Options).FirstOrDefaultAsync(q => q.Id == id && q.OrganizationId == ctx.OrganizationId);
        if (question == null) return NotFound();
        return Ok(question);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQuestionRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        subscriptions.EnforceFeature("questionBank");
        var ctx = context.Get();
        var type = Enum.Parse<QuestionType>(request.Type.Replace(" ", ""), true);
        var question = new Question
        {
            OrganizationId = ctx.OrganizationId,
            CategoryId = request.CategoryId,
            Type = type,
            Text = request.Text,
            ImageUrl = request.ImageUrl,
            OrderIndex = request.OrderIndex
        };
        db.Questions.Add(question);
        await db.SaveChangesAsync();
        if (request.Options != null)
        {
            foreach (var opt in request.Options)
            {
                db.QuestionOptions.Add(new QuestionOption
                {
                    QuestionId = question.Id,
                    Text = opt.Text,
                    IsCorrect = opt.IsCorrect,
                    OrderIndex = opt.OrderIndex,
                    MatchKey = opt.MatchKey
                });
            }
            await db.SaveChangesAsync();
        }
        return Ok(question);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateQuestionRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        subscriptions.EnforceFeature("questionBank");
        var ctx = context.Get();
        var question = await db.Questions.IgnoreQueryFilters().FirstOrDefaultAsync(q => q.Id == id && q.OrganizationId == ctx.OrganizationId);
        if (question == null) return NotFound();
        if (request.Text != null) question.Text = request.Text;
        if (request.ImageUrl != null) question.ImageUrl = request.ImageUrl;
        if (request.OrderIndex.HasValue) question.OrderIndex = request.OrderIndex.Value;
        if (request.Options != null)
        {
            var existing = await db.QuestionOptions.Where(o => o.QuestionId == id).ToListAsync();
            db.QuestionOptions.RemoveRange(existing);
            foreach (var opt in request.Options)
            {
                db.QuestionOptions.Add(new QuestionOption
                {
                    QuestionId = id,
                    Text = opt.Text,
                    IsCorrect = opt.IsCorrect,
                    OrderIndex = opt.OrderIndex,
                    MatchKey = opt.MatchKey
                });
            }
        }
        await db.SaveChangesAsync();
        return Ok(question);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        subscriptions.EnforceFeature("questionBank");
        var ctx = context.Get();
        var question = await db.Questions.IgnoreQueryFilters().FirstOrDefaultAsync(q => q.Id == id && q.OrganizationId == ctx.OrganizationId);
        if (question == null) return NotFound();
        question.IsDeleted = true;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
