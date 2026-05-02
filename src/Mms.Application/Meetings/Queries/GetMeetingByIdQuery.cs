using MediatR;
using Mms.Application.Meetings.Dtos;

namespace Mms.Application.Meetings.Queries;

public record GetMeetingByIdQuery(Guid Id) : IRequest<MeetingDetailDto?>;
