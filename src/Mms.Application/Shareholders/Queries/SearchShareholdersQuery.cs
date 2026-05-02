using MediatR;

namespace Mms.Application.Shareholders.Queries;

/// <summary>Tra cứu CĐ theo tên, CCCD hoặc mã CĐ — dùng cho autocomplete.</summary>
public record SearchShareholdersQuery(Guid MeetingId, string SearchTerm)
    : IRequest<IList<ShareholderSearchResultDto>>;

public record ShareholderSearchResultDto(
    Guid Id,
    string FullName,
    string IdNumber,
    long VotingRights);
