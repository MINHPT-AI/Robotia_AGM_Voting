using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Meetings.Dtos;
using Mms.Application.Meetings.Queries;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Meetings;

public class GetMeetingByIdHandler : IRequestHandler<GetMeetingByIdQuery, MeetingDetailDto?>
{
    private readonly MmsDbContext _db;
    public GetMeetingByIdHandler(MmsDbContext db) => _db = db;

    public async Task<MeetingDetailDto?> Handle(GetMeetingByIdQuery q, CancellationToken ct)
    {
        var m = await _db.Meetings
            .AsNoTracking()
            .Include(x => x.Resolutions.OrderBy(r => r.DisplayOrder))
            .Include(x => x.Candidates.OrderBy(c => c.DisplayOrder))
            .FirstOrDefaultAsync(x => x.Id == q.Id, ct);

        if (m is null) return null;

        return new MeetingDetailDto(
            m.Id, m.CompanyId, m.Title, m.MeetingType, m.Status,
            m.MeetingDate, m.Location, m.RecordDate, m.TotalVotingShares,
            m.Chairman, m.Secretary, m.Notes,
            m.Resolutions.Select(r =>
                new ResolutionDto(r.Id, r.DisplayOrder, r.Title, r.Content)).ToList(),
            m.Candidates.Select(c =>
                new CandidateDto(c.Id, c.DisplayOrder, c.FullName,
                    c.Position, c.CurrentPosition, c.BirthYear, c.Notes)).ToList()
        );
    }
}
