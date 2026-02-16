using Microsoft.AspNetCore.Mvc;
using Quizory.Api.Domain;
using Quizory.Api.Services;

namespace Quizory.Api.Controllers;

[ApiController]
[Route("api/export")]
public class ExportController(IRequestContextAccessor context, IExportImportService exportImport, IOrgAuthorizationService auth) : ControllerBase
{
    [HttpGet("template/excel")]
    public async Task<IActionResult> GetImportTemplate([FromQuery] string? templateVersion)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        var bytes = await exportImport.GetImportTemplateAsync(templateVersion ?? "1.0");
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "quiz-import-template.xlsx");
    }

    [HttpGet("quiz/{quizId:guid}/excel")]
    public async Task<IActionResult> ExportQuizExcel(Guid quizId)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        var ctx = context.Get();
        var bytes = await exportImport.ExportQuizToExcelAsync(ctx.OrganizationId, quizId);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"quiz-{quizId}.xlsx");
    }

    [HttpGet("quiz/{quizId:guid}/pdf")]
    public async Task<IActionResult> ExportQuizPdf(Guid quizId)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        var ctx = context.Get();
        var bytes = await exportImport.ExportQuizToPdfAsync(ctx.OrganizationId, quizId);
        return File(bytes, "application/pdf", $"quiz-{quizId}.pdf");
    }

    [HttpGet("league/{leagueId:guid}/excel")]
    public async Task<IActionResult> ExportLeagueExcel(Guid leagueId)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        var ctx = context.Get();
        var bytes = await exportImport.ExportLeagueToExcelAsync(ctx.OrganizationId, leagueId);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"league-{leagueId}.xlsx");
    }

    [HttpPost("import/excel")]
    public async Task<IActionResult> ImportExcel([FromQuery] string templateVersion, IFormFile file)
    {
        auth.EnsureAtLeast(OrganizationRole.Admin);
        if (file == null || file.Length == 0) return BadRequest(new { errorCode = "NoFile" });
        if (string.IsNullOrEmpty(templateVersion)) templateVersion = "1.0";
        var ctx = context.Get();
        await using var stream = file.OpenReadStream();
        var (success, error) = await exportImport.ImportFromExcelAsync(ctx.OrganizationId, stream, templateVersion);
        if (!success) return BadRequest(new { errorCode = "ImportFailed", message = error });
        return Ok(new { message = "Import successful" });
    }
}
