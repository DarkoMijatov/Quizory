using Quizory.Api.Data;
using Quizory.Api.Domain;

namespace Quizory.Api.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entityType, string? entityId, object? payload = null);
}

public class AuditService(IRequestContextAccessor context, AppDbContext db) : IAuditService
{
    public async Task LogAsync(string action, string entityType, string? entityId, object? payload = null)
    {
        var ctx = context.Get();
        var log = new AuditLog
        {
            OrganizationId = ctx.OrganizationId,
            UserId = ctx.UserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Payload = payload == null ? "{}" : System.Text.Json.JsonSerializer.Serialize(payload)
        };
        db.AuditLogs.Add(log);
        await db.SaveChangesAsync();
    }
}
