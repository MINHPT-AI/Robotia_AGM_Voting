using MediatR;

namespace Mms.Application.Meetings.Commands;

public record DeleteMeetingCommand(Guid Id) : IRequest;
