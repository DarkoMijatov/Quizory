using Quizory.Api.Domain;

namespace Quizory.Api.Dtos;

// Auth
public record RegisterRequest(string Email, string Password, string? DisplayName, string? OrganizationName);
public record LoginRequest(string Email, string Password);
public record AuthResult(bool Success, string? Token, Guid? UserId, string? Email, string? DisplayName, string? PreferredLanguage, Guid? OrganizationId, string? Role, string? ErrorCode);
public record ResetPasswordRequest(string Token, string NewPassword);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public static class AuthResultFactory
{
    public static AuthResult Ok(string token, Guid userId, string email, string displayName, string preferredLanguage, Guid orgId, OrganizationRole role) =>
        new(true, token, userId, email, displayName, preferredLanguage, orgId, role.ToString(), null);
    public static AuthResult Fail(string errorCode) =>
        new(false, null, null, null, null, null, null, null, errorCode);
}

// Organizations & members
public record InviteMemberRequest(string Email, string DisplayName, string Role);
public record OrganizationLanguageRequest(string PreferredLanguage);
public record UpdateOrganizationRequest(string? Name, string? PrimaryColor);
public record UpdateMemberRoleRequest(string Role);
public record PaginatedResponse<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

// Teams
public record CreateTeamRequest(string Name);
public record UpdateTeamRequest(string Name);
public record AddTeamAliasRequest(string Alias, Guid? QuizId);

// Categories
public record CreateCategoryRequest(string Name);
public record UpdateCategoryRequest(string Name);

// Leagues
public record CreateLeagueRequest(string Name);
public record UpdateLeagueRequest(string Name);

// Global settings
public record UpdateGlobalSettingsRequest(int? DefaultCategoriesCount, int? DefaultQuestionsPerCategory);

// Quiz
public record CreateQuizRequest(string Name, DateTime DateUtc, string Location, Guid? LeagueId, List<Guid> CategoryIds, List<Guid> TeamIds, Dictionary<Guid, string?>? TeamAliasesInQuiz);
public record UpdateQuizRequest(string? Name, DateTime? DateUtc, string? Location, Guid? LeagueId, QuizStatus? Status, int? OverrideCategoriesCount, int? OverrideQuestionsPerCategory);
public record UpdateScoreRequest(Guid TeamId, Guid CategoryId, int Points, int BonusPoints, string Notes, bool IsLocked);
public record ApplyHelpRequest(Guid TeamId, Guid HelpTypeId);

// Helps
public record CreateHelpTypeRequest(string Name, string Behavior);
public record UpdateHelpTypeRequest(string? Name, string? Behavior);

// Question bank
public record CreateQuestionRequest(Guid CategoryId, string Type, string Text, string? ImageUrl, int OrderIndex, List<QuestionOptionDto>? Options);
public record UpdateQuestionRequest(string? Text, string? ImageUrl, int? OrderIndex, List<QuestionOptionDto>? Options);
public record QuestionOptionDto(string Text, bool IsCorrect, int OrderIndex, string? MatchKey);

// Statistics
public record StatsFilterRequest(DateTime? From, DateTime? To, Guid? LeagueId, Guid? TeamId, Guid? CategoryId);
public record QuizSummaryDto(Guid Id, string Name, DateTime DateUtc, string Location, string Status, int TeamCount, int CategoryCount, Guid? WinnerTeamId, int? WinnerPoints);
public record LeagueSummaryDto(Guid Id, string Name, int QuizCount, List<TeamRankDto> TopTeams);
public record TeamRankDto(Guid TeamId, string TeamName, int Rank, int Points);
public record CategoryStatsDto(Guid CategoryId, string CategoryName, double AveragePoints, int QuizCount);
public record ExportRequest(string Format, Guid? QuizId, Guid? LeagueId, DateTime? From, DateTime? To);

// Share
public record CreateShareTokenRequest(Guid QuizId, DateTime? ExpiresAtUtc);
public record ShareLeaderboardDto(List<TeamRankDto> Rankings, string QuizName, DateTime QuizDate, string? PrimaryColor);
