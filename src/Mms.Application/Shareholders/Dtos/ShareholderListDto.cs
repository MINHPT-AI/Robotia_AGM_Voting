namespace Mms.Application.Shareholders.Dtos;

public record ShareholderListDto(
    Guid Id,
    int DisplayOrder,
    string VsdcRow,
    string FullName,
    string? Sid,
    string? InvestorCode,
    string IdNumber,
    DateOnly? IdIssueDate,
    string? Address,
    string? Email,
    string? Phone,
    string? Nationality,
    long SharesNonDeposit,
    long SharesDeposit,
    long SharesTotal,
    long RightsNonDeposit,
    long RightsDeposit,
    long VotingRights,
    bool IsOrganization,
    bool IsForeign);
