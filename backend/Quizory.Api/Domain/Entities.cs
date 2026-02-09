namespace Quizory.Api.Domain;

public enum OrganizationRole { Owner, Admin, User }
public enum SubscriptionPlan { Free, Trial, Premium }
public enum QuizStatus { Draft, Live, Finished }
public enum HelpBehavior { DoubleScore, MarkerOnly }

public abstract class TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = "sr";
    public bool IsEmailVerified { get; set; }
}

public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.Trial;
    public DateTime TrialEndsAtUtc { get; set; } = DateTime.UtcNow.AddDays(14);
    public string PrimaryColor { get; set; } = "#5E35B1";
}

public class Membership
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public OrganizationRole Role { get; set; }
}

public class Team : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<TeamAlias> Aliases { get; set; } = new List<TeamAlias>();
}

public class TeamAlias : TenantEntity
{
    public Guid TeamId { get; set; }
    public Guid? QuizId { get; set; }
    public string Alias { get; set; } = string.Empty;
}

public class Category : TenantEntity
{
    public string Name { get; set; } = string.Empty;
}

public class League : TenantEntity
{
    public string Name { get; set; } = string.Empty;
}

public class Quiz : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime DateUtc { get; set; }
    public string Location { get; set; } = string.Empty;
    public QuizStatus Status { get; set; } = QuizStatus.Draft;
    public Guid? LeagueId { get; set; }
    public int? OverrideCategoriesCount { get; set; }
    public int? OverrideQuestionsPerCategory { get; set; }
}

public class QuizTeam : TenantEntity
{
    public Guid QuizId { get; set; }
    public Guid TeamId { get; set; }
}

public class HelpType : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public HelpBehavior Behavior { get; set; }
}

public class HelpUsage : TenantEntity
{
    public Guid QuizId { get; set; }
    public Guid TeamId { get; set; }
    public Guid HelpTypeId { get; set; }
}

public class ScoreEntry : TenantEntity
{
    public Guid QuizId { get; set; }
    public Guid TeamId { get; set; }
    public Guid CategoryId { get; set; }
    public int Points { get; set; }
    public int BonusPoints { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
}

public class AuditLog : TenantEntity
{
    public string Action { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Payload { get; set; } = "{}";
}
