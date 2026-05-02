using MediatR;
using Mms.Application.Common.Models;
using Mms.Application.Meetings.Dtos;
using Mms.Domain.Enums;

namespace Mms.Application.Meetings.Queries;

public record GetMeetingsQuery(
    int? Year = null,
    MeetingStatus? Status = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<MeetingListItemDto>>;
