using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Shareholders.Queries;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Shareholders;

public class GetExistingShareholderIdsHandler
    : IRequestHandler<GetExistingShareholderIdsQuery, HashSet<string>>
{
    private readonly MmsDbContext _db;

    public GetExistingShareholderIdsHandler(MmsDbContext db) => _db = db;

    public async Task<HashSet<string>> Handle(
        GetExistingShareholderIdsQuery req, CancellationToken ct)
    {
        var ids = await _db.Shareholders
            .Where(s => s.MeetingId == req.MeetingId)
            .Select(s => s.IdNumber)
            .ToListAsync(ct);

        return new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
    }
}
