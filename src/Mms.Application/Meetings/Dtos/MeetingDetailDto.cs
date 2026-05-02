using Mms.Domain.Enums;

namespace Mms.Application.Meetings.Dtos;

public record MeetingDetailDto(
    Guid Id,
    Guid CompanyId,
    string Title,
    MeetingType MeetingType,
    MeetingStatus Status,
    DateTime MeetingDate,
    string Location,
    DateOnly RecordDate,
    long TotalVotingShares,
    string? Chairman,
    string? Secretary,
    string? Notes,
    List<ResolutionDto> Resolutions,
    List<CandidateDto> Candidates
);
