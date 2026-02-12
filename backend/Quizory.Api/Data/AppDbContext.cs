using Microsoft.EntityFrameworkCore;
using Quizory.Api.Domain;

namespace Quizory.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamAlias> TeamAliases => Set<TeamAlias>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<League> Leagues => Set<League>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizTeam> QuizTeams => Set<QuizTeam>();
    public DbSet<HelpType> HelpTypes => Set<HelpType>();
    public DbSet<HelpUsage> HelpUsages => Set<HelpUsage>();
    public DbSet<ScoreEntry> ScoreEntries => Set<ScoreEntry>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Membership>().HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique();
        modelBuilder.Entity<TeamAlias>().HasIndex(x => new { x.OrganizationId, x.QuizId, x.Alias }).IsUnique();
        modelBuilder.Entity<HelpUsage>().HasIndex(x => new { x.OrganizationId, x.QuizId, x.TeamId, x.HelpTypeId }).IsUnique();
        modelBuilder.Entity<ScoreEntry>().HasIndex(x => new { x.OrganizationId, x.QuizId, x.TeamId, x.CategoryId }).IsUnique();
    }
}
