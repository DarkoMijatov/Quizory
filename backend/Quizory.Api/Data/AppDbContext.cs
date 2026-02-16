using Microsoft.EntityFrameworkCore;
using Quizory.Api.Domain;

namespace Quizory.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<GlobalSettings> GlobalSettings => Set<GlobalSettings>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamAlias> TeamAliases => Set<TeamAlias>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<League> Leagues => Set<League>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizCategory> QuizCategories => Set<QuizCategory>();
    public DbSet<QuizTeam> QuizTeams => Set<QuizTeam>();
    public DbSet<HelpType> HelpTypes => Set<HelpType>();
    public DbSet<HelpUsage> HelpUsages => Set<HelpUsage>();
    public DbSet<ScoreEntry> ScoreEntries => Set<ScoreEntry>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<PublicShareToken> PublicShareTokens => Set<PublicShareToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => new { x.ExternalAuthProvider, x.ExternalAuthId }).IsUnique().HasFilter("ExternalAuthProvider IS NOT NULL");
        });
        modelBuilder.Entity<EmailVerificationToken>(e =>
        {
            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => x.UserId);
        });
        modelBuilder.Entity<PasswordResetToken>(e =>
        {
            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => x.UserId);
        });
        modelBuilder.Entity<Membership>().HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique();
        modelBuilder.Entity<Payment>(e =>
        {
            e.HasIndex(x => x.OrganizationId);
            e.HasIndex(x => x.ExternalPaymentId).HasFilter("ExternalPaymentId IS NOT NULL");
        });
        modelBuilder.Entity<GlobalSettings>().HasIndex(x => x.OrganizationId).IsUnique();
        modelBuilder.Entity<TeamAlias>().HasIndex(x => new { x.OrganizationId, x.QuizId, x.Alias }).IsUnique();
        modelBuilder.Entity<HelpUsage>().HasIndex(x => new { x.OrganizationId, x.QuizId, x.TeamId, x.HelpTypeId }).IsUnique();
        modelBuilder.Entity<ScoreEntry>().HasIndex(x => new { x.OrganizationId, x.QuizId, x.TeamId, x.CategoryId }).IsUnique();

        modelBuilder.Entity<QuizCategory>(e =>
        {
            e.HasKey(x => new { x.QuizId, x.CategoryId });
            e.HasIndex(x => x.QuizId);
        });
        modelBuilder.Entity<PublicShareToken>(e =>
        {
            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => new { x.OrganizationId, x.QuizId });
        });

        // Soft delete filter for tenant entities
        modelBuilder.Entity<Team>().HasQueryFilter(t => !t.IsDeleted);
        modelBuilder.Entity<TeamAlias>().HasQueryFilter(t => !t.IsDeleted);
        modelBuilder.Entity<Category>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<League>().HasQueryFilter(l => !l.IsDeleted);
        modelBuilder.Entity<Quiz>().HasQueryFilter(q => !q.IsDeleted);
        modelBuilder.Entity<QuizTeam>().HasQueryFilter(q => !q.IsDeleted);
        modelBuilder.Entity<HelpType>().HasQueryFilter(h => !h.IsDeleted);
        modelBuilder.Entity<HelpUsage>().HasQueryFilter(h => !h.IsDeleted);
        modelBuilder.Entity<ScoreEntry>().HasQueryFilter(s => !s.IsDeleted);
        modelBuilder.Entity<Question>().HasQueryFilter(q => !q.IsDeleted);
        modelBuilder.Entity<AuditLog>().HasQueryFilter(a => !a.IsDeleted);
    }

    public override int SaveChanges()
    {
        foreach (var entry in ChangeTracker.Entries<TenantEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Property(nameof(TenantEntity.UpdatedAtUtc)).CurrentValue = DateTime.UtcNow;
        }
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<TenantEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Property(nameof(TenantEntity.UpdatedAtUtc)).CurrentValue = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
