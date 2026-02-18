using Microsoft.EntityFrameworkCore;
using Quizory.Api.Data;
using Quizory.Api.Domain;
using Quizory.Api.Dtos;

namespace Quizory.Api.Services;

public interface IPaymentService
{
    Task<PaymentDto> CreatePaymentAsync(Guid organizationId, CreatePaymentRequest request);
    Task<PaymentDto?> GetPaymentAsync(Guid paymentId, Guid organizationId);
    Task<(List<PaymentDto> Items, int Total)> ListPaymentsAsync(Guid organizationId, int page, int pageSize);
    Task<PaymentDto?> ConfirmPaymentAsync(Guid organizationId, Guid? paymentId, string? externalPaymentId);
}

public class PaymentService(IRequestContextAccessor context, AppDbContext db, ISubscriptionService subscriptionService, ITextLocalizer localizer) : IPaymentService
{
    public async Task<PaymentDto> CreatePaymentAsync(Guid organizationId, CreatePaymentRequest request)
    {
        var ctx = context.Get();
        if (ctx.OrganizationId != organizationId) throw new UnauthorizedAccessException(localizer.T("Forbidden"));
        if (ctx.Role != OrganizationRole.Owner) throw new UnauthorizedAccessException(localizer.T("OwnerOnly"));
        var org = await db.Organizations.FindAsync(organizationId);
        if (org == null) throw new InvalidOperationException(localizer.T("OrganizationNotFound"));
        var plan = ParsePlan(request.Plan);
        if (plan != SubscriptionPlan.Premium)
            throw new InvalidOperationException(localizer.T("PaymentOnlyForPremium"));
        if (request.Amount <= 0)
            throw new InvalidOperationException(localizer.T("PaymentAmountInvalid"));
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency.Trim().ToUpperInvariant();
        var payment = new Payment
        {
            OrganizationId = organizationId,
            Amount = request.Amount,
            Currency = currency,
            Status = PaymentStatus.Pending,
            Plan = plan
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        return ToDto(payment, clientSecret: null);
    }

    public async Task<PaymentDto?> GetPaymentAsync(Guid paymentId, Guid organizationId)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId && p.OrganizationId == organizationId);
        return payment == null ? null : ToDto(payment, null);
    }

    public async Task<(List<PaymentDto> Items, int Total)> ListPaymentsAsync(Guid organizationId, int page, int pageSize)
    {
        var query = db.Payments.Where(p => p.OrganizationId == organizationId);
        var total = await query.CountAsync();
        var list = await query.OrderByDescending(p => p.CreatedAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (list.Select(p => ToDto(p, null)).ToList(), total);
    }

    public async Task<PaymentDto?> ConfirmPaymentAsync(Guid organizationId, Guid? paymentId, string? externalPaymentId)
    {
        var ctx = context.Get();
        if (ctx.OrganizationId != organizationId) throw new UnauthorizedAccessException(localizer.T("Forbidden"));
        if (ctx.Role != OrganizationRole.Owner) throw new UnauthorizedAccessException(localizer.T("OwnerOnly"));
        Payment? payment = null;
        if (paymentId.HasValue)
            payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId.Value && p.OrganizationId == organizationId);
        if (payment == null && !string.IsNullOrWhiteSpace(externalPaymentId))
            payment = await db.Payments.FirstOrDefaultAsync(p => p.ExternalPaymentId == externalPaymentId && p.OrganizationId == organizationId);
        if (payment == null) return null;
        if (payment.Status != PaymentStatus.Pending)
            throw new InvalidOperationException(localizer.T("PaymentAlreadyProcessed"));
        payment.Status = PaymentStatus.Completed;
        payment.CompletedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await subscriptionService.SetPremiumAsync(organizationId);
        return ToDto(payment, null);
    }

    private static SubscriptionPlan ParsePlan(string plan)
    {
        return Enum.TryParse<SubscriptionPlan>(plan, true, out var p) ? p : throw new ArgumentException("Invalid plan.");
    }

    private static PaymentDto ToDto(Payment p, string? clientSecret) => new(
        p.Id, p.OrganizationId, p.Amount, p.Currency, p.Status.ToString(), p.Plan.ToString(),
        p.ExternalPaymentId, p.CreatedAtUtc, p.CompletedAtUtc, clientSecret);
}
