using Mms.Domain.Enums;

namespace Mms.Application.Meetings.Dtos;

public record MeetingListItemDto(
    Guid Id,
    string Title,
    MeetingType MeetingType,
    MeetingStatus Status,
    DateTime MeetingDate,
    DateOnly RecordDate,
    long TotalVotingShares,
    int ShareholderCount
);
