using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Common.Models;
using Mms.Application.Meetings.Dtos;
using Mms.Application.Meetings.Queries;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Meetings;

public class GetMeetingsHandler : IRequestHandler<GetMeetingsQuery, PagedResult<MeetingListItemDto>>
{
    private readonly MmsDbContext _db;
    public GetMeetingsHandler(MmsDbContext db) => _db = db;

    public async Task<PagedResult<MeetingListItemDto>> Handle(
        GetMeetingsQuery q, CancellationToken ct)
    {
        var query = _db.Meetings.AsNoTracking();

        if (q.Year.HasValue)
            query = query.Where(m => m.MeetingDate.Year == q.Year.Value);
        if (q.Status.HasValue)
            query = query.Where(m => m.Status == q.Status.Value);
        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(m => m.Title.Contains(q.Search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(m => m.MeetingDate)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(m => new MeetingListItemDto(
                m.Id, m.Title, m.MeetingType, m.Status,
                m.MeetingDate, m.RecordDate, m.TotalVotingShares,
                m.Shareholders.Count))
            .ToListAsync(ct);

        return new PagedResult<MeetingListItemDto>(items, total, q.Page, q.PageSize);
    }
}
