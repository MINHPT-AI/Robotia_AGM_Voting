using MediatR;

namespace Mms.Application.Meetings.Commands;

public record CloneMeetingCommand(Guid SourceId) : IRequest<Guid>;
