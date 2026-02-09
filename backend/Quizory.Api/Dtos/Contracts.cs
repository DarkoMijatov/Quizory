namespace Quizory.Api.Dtos;

public record CreateQuizRequest(string Name, DateTime DateUtc, string Location, Guid? LeagueId, List<Guid> CategoryIds, List<Guid> TeamIds);
public record UpdateScoreRequest(Guid TeamId, Guid CategoryId, int Points, int BonusPoints, string Notes, bool IsLocked);
public record ApplyHelpRequest(Guid TeamId, Guid HelpTypeId);
public record InviteMemberRequest(string Email, string DisplayName, string Role);
public record OrganizationLanguageRequest(string PreferredLanguage);
