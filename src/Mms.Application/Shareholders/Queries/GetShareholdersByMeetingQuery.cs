using MediatR;
using Mms.Application.Shareholders.Dtos;

namespace Mms.Application.Shareholders.Queries;

public record GetShareholdersByMeetingQuery(Guid MeetingId)
    : IRequest<List<ShareholderListDto>>;
