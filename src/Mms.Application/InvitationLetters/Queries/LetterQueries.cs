using MediatR;
using Mms.Application.InvitationLetters.Dtos;
using Mms.Domain.Entities;

namespace Mms.Application.InvitationLetters.Queries;

// ── Paged list with status filter and search ──
public record GetLettersQuery(
    Guid MeetingId,
    InvitationStatus? StatusFilter,
    string? SearchTerm,
    int Page = 1,
    int PageSize = 50) : IRequest<GetLettersResult>;

public record GetLettersResult(
    IList<LetterListItem> Items,
    int TotalCount);

// ── Stats by status for a meeting ──
public record GetLetterStatsQuery(Guid MeetingId) : IRequest<LetterStatsDto>;
