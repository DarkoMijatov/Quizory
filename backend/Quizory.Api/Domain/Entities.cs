namespace Quizory.Api.Domain;

public enum OrganizationRole { Owner, Admin, User }
public enum SubscriptionPlan { Free, Trial, Premium }
public enum QuizStatus { Draft, Live, Finished }
public enum HelpBehavior { DoubleScore, MarkerOnly }
public enum QuestionType { Text, MultipleChoice, Matching, Image }

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
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Optional: for future OAuth (Google/Microsoft).</summary>
    public string? ExternalAuthProvider { get; set; }
    public string? ExternalAuthId { get; set; }
}

public class EmailVerificationToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class PasswordResetToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool Used { get; set; }
}

public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.Trial;
    public DateTime? TrialEndsAtUtc { get; set; }
    public string PrimaryColor { get; set; } = "#5E35B1";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Membership
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public OrganizationRole Role { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class GlobalSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public int DefaultCategoriesCount { get; set; } = 6;
    public int DefaultQuestionsPerCategory { get; set; } = 5;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
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

public class QuizCategory
{
    public Guid QuizId { get; set; }
    public Guid CategoryId { get; set; }
    public int OrderIndex { get; set; }
}

public class QuizTeam : TenantEntity
{
    public Guid QuizId { get; set; }
    public Guid TeamId { get; set; }
    /// <summary>Display alias for this team in this quiz (optional override).</summary>
    public string? AliasInQuiz { get; set; }
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

/// <summary>Question bank (Premium). Attached to organization category.</summary>
public class Question : TenantEntity
{
    public Guid CategoryId { get; set; }
    public QuestionType Type { get; set; }
    public string Text { get; set; } = string.Empty;
    /// <summary>Optional image URL or base64 for Image type.</summary>
    public string? ImageUrl { get; set; }
    public int OrderIndex { get; set; }
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
}

public class QuestionOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public Question? Question { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }
    /// <summary>For Matching: pair key (e.g. "A" matches "1").</summary>
    public string? MatchKey { get; set; }
}

/// <summary>Public share token for read-only leaderboard (Premium).</summary>
public class PublicShareToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid QuizId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class AuditLog : TenantEntity
{
    public string Action { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Payload { get; set; } = "{}";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
