using MediatR;
using Mms.Application.Meetings.Dtos;
using Mms.Domain.Enums;

namespace Mms.Application.Meetings.Commands;

public record UpdateMeetingCommand(
    Guid Id,
    string Title,
    MeetingType MeetingType,
    DateTime MeetingDate,
    string Location,
    DateOnly RecordDate,
    long TotalVotingShares,
    string? Chairman,
    string? Secretary,
    string? Notes,
    List<ResolutionDto> Resolutions,
    List<CandidateDto> Candidates
) : IRequest;
