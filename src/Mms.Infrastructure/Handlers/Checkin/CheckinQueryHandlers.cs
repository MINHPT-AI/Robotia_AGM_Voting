using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Checkin.Dtos;
using Mms.Application.Checkin.Queries;
using Mms.Domain.Enums;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Checkin;

public class GetCheckinTopbarHandler : IRequestHandler<GetCheckinTopbarQuery, CheckinTopbarDto>
{
    private readonly MmsDbContext _db;
    private readonly ISender _mediator;

    public GetCheckinTopbarHandler(MmsDbContext db, ISender mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<CheckinTopbarDto> Handle(GetCheckinTopbarQuery query, CancellationToken ct)
    {
        var meeting = await _db.Meetings.FindAsync(new object[] { query.MeetingId }, ct)
            ?? throw new InvalidOperationException("Cuộc họp không tồn tại.");

        // ── Dòng 1 — Số cổ đông ──
        var totalVsdcShareholders = await _db.Shareholders
            .CountAsync(s => s.MeetingId == query.MeetingId, ct);

        // Sử dụng chung logic với DS1 để đảm bảo đồng nhất tuyệt đối
        var ds1 = await _mediator.Send(new GetAttendanceListQuery(query.MeetingId), ct);

        var attendingShareholders = ds1.Count;

        // ── Dòng 2 — Số CP biểu quyết ──
        var totalVsdcShares = meeting.TotalVotingShares;

        // CP dự họp = tổng TotalShares của DS1
        var attendingShares = ds1.Sum(x => x.TotalShares);

        // Tỷ lệ
        var quorumPct = totalVsdcShares > 0
            ? (decimal)attendingShares / totalVsdcShares * 100m
            : 0m;

        var isQuorumReached = quorumPct >= (meeting.QuorumThreshold * 100);

        // ── Dòng 3 — DS2 phụ ──
        var activeAttendances = await _db.AttendanceRecords
            .Where(a => a.MeetingId == query.MeetingId && a.IsActive)
            .ToListAsync(ct);

        var physicalAttendees = activeAttendances
            .Select(a => a.PhysicalAttendeeIdNumber)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct().Count();

        var totalBallots = await _db.Ballots
            .Where(b => b.MeetingId == query.MeetingId
                     && b.AttendanceRecord != null && b.AttendanceRecord.IsActive
                     && b.Status != BallotStatus.Invalidated)
            .Select(b => b.AttendCode)
            .Distinct()
            .CountAsync(ct);

        return new CheckinTopbarDto(
            totalVsdcShareholders, attendingShareholders,
            totalVsdcShares, attendingShares,
            Math.Round(quorumPct, 2), isQuorumReached,
            physicalAttendees, totalBallots);
    }
}

public class GetReprintQueueHandler : IRequestHandler<GetReprintQueueQuery, IList<ReprintQueueItemDto>>
{
    private readonly MmsDbContext _db;
    public GetReprintQueueHandler(MmsDbContext db) => _db = db;

    public async Task<IList<ReprintQueueItemDto>> Handle(GetReprintQueueQuery query, CancellationToken ct)
    {
        return await _db.Ballots
            .Where(b => b.MeetingId == query.MeetingId && b.ReprintNeeded)
            .OrderByDescending(b => b.InvalidatedAt)
            .Select(b => new ReprintQueueItemDto(
                b.Id, b.BallotType, b.AttendCode, b.Shareholder.FullName,
                b.InvalidationReason ?? "Không rõ", b.InvalidatedAt ?? DateTime.UtcNow))
            .ToListAsync(ct);
    }
}

public class GetIssuedBallotsHandler : IRequestHandler<GetIssuedBallotsQuery, IList<BallotIssuedDto>>
{
    private readonly MmsDbContext _db;
    public GetIssuedBallotsHandler(MmsDbContext db) => _db = db;

    public async Task<IList<BallotIssuedDto>> Handle(GetIssuedBallotsQuery query, CancellationToken ct)
    {
        return await _db.Ballots
            .Where(b => b.AttendanceRecordId == query.AttendanceRecordId
                     && b.Status != BallotStatus.Invalidated)
            .OrderBy(b => b.BallotType).ThenBy(b => b.SplitSequence)
            .Select(b => new BallotIssuedDto(
                b.Id, b.BallotType, b.AttendCode,
                b.VotingShares, b.SplitSequence, b.Status))
            .ToListAsync(ct);
    }
}
