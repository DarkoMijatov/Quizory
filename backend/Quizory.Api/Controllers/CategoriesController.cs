using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizory.Api.Data;
using Quizory.Api.Domain;
using Quizory.Api.Dtos;
using Quizory.Api.Services;

namespace Quizory.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(AppDbContext db, IRequestContextAccessor context, IOrgAuthorizationService auth) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        var ctx = context.Get();
        var query = db.Categories.Where(c => c.OrganizationId == ctx.OrganizationId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search));
        var total = await query.CountAsync();
        var items = await query.OrderBy(c => c.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PaginatedResponse<Category>(items, total, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        auth.EnsureAtLeast(OrganizationRole.User);
        var ctx = context.Get();
        var cat = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == ctx.OrganizationId);
        if (cat == null) return NotFound();
        return Ok(cat);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        var ctx = context.Get();
        var category = new Category { OrganizationId = ctx.OrganizationId, Name = request.Name };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return Ok(category);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        var ctx = context.Get();
        var cat = await db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == ctx.OrganizationId);
        if (cat == null) return NotFound();
        cat.Name = request.Name;
        await db.SaveChangesAsync();
        return Ok(cat);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        var ctx = context.Get();
        var cat = await db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == ctx.OrganizationId);
        if (cat == null) return NotFound();
        cat.IsDeleted = true;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
