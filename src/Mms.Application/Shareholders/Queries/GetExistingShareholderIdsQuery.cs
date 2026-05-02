using MediatR;

namespace Mms.Application.Shareholders.Queries;

public record GetExistingShareholderIdsQuery(Guid MeetingId)
    : IRequest<HashSet<string>>;
