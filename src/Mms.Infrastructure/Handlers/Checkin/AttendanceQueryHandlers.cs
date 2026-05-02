using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Checkin.Dtos;
using Mms.Application.Checkin.Queries;
using Mms.Application.Common.Interfaces;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Checkin;

public class GetAttendanceListHandler
    : IRequestHandler<GetAttendanceListQuery, IList<AttendanceListItemDto>>
{
    private readonly MmsDbContext _db;
    public GetAttendanceListHandler(MmsDbContext db) => _db = db;

    public async Task<IList<AttendanceListItemDto>> Handle(
        GetAttendanceListQuery query, CancellationToken ct)
    {
        // IDs cổ đông đã check-in trực tiếp
        var directCheckedInIds = await _db.AttendanceRecords
            .Where(a => a.MeetingId == query.MeetingId && a.IsActive)
            .Select(a => a.ShareholderId)
            .ToListAsync(ct);

        // Proxy outgoing CONFIRMED: CĐ ủy quyền đi cho người nhận đã check-in
        var activeStatuses = new[] { ProxyStatus.Confirmed };

        // Lấy tất cả proxy CONFIRMED trong cuộc họp
        var confirmedProxies = await _db.Proxies
            .Where(p => p.MeetingId == query.MeetingId && activeStatuses.Contains(p.Status))
            .Select(p => new {
                p.GrantorId,
                p.GranteeName,
                p.GranteeIdNumber,
                p.GranteeShareholderId,
                p.Shares
            })
            .ToListAsync(ct);

        // Xác định CĐ nào ủy quyền cho người nhận đã check-in
        var proxyAttendingGrantorIds = confirmedProxies
            .Where(p => p.GranteeShareholderId != null && directCheckedInIds.Contains(p.GranteeShareholderId.Value))
            .Select(p => p.GrantorId)
            .Distinct()
            .ToList();

        // DS1 = union of directCheckedInIds + proxyAttendingGrantorIds
        var allAttendingIds = directCheckedInIds.Union(proxyAttendingGrantorIds).Distinct().ToList();

        // Lấy thông tin shareholders
        var shareholders = await _db.Shareholders
            .Where(s => s.MeetingId == query.MeetingId && allAttendingIds.Contains(s.Id))
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(ct);

        var result = new List<AttendanceListItemDto>();

        foreach (var sh in shareholders)
        {
            var isDirect = directCheckedInIds.Contains(sh.Id);

            // Proxy outgoing TẤT CẢ (Confirmed)
            var allOutgoingProxies = confirmedProxies
                .Where(p => p.GrantorId == sh.Id)
                .ToList();
            var totalDelegated = allOutgoingProxies.Sum(p => p.Shares);

            // Proxy outgoing đã check-in (chỉ hiển thị trong cột CP ủy quyền khi người nhận đã đến)
            var attendingOutgoingProxies = allOutgoingProxies
                .Where(p => p.GranteeShareholderId != null
                         && directCheckedInIds.Contains(p.GranteeShareholderId.Value))
                .ToList();

            var proxyShares = attendingOutgoingProxies.Sum(p => p.Shares);
            
            // CĐ trực tiếp check-in thì CP trực tiếp = Tổng Quyền - Tổng ĐÃ Ủy quyền đi
            var directShares = isDirect ? (sh.VotingRights - totalDelegated) : 0;
            if (directShares < 0) directShares = 0;

            string attendanceMode;
            if (isDirect && proxyShares > 0)
                attendanceMode = "Trực tiếp + Ủy quyền";
            else if (isDirect)
                attendanceMode = "Trực tiếp";
            else
                attendanceMode = "Ủy quyền";

            var delegations = attendingOutgoingProxies.Select(p => new ProxyDelegationDto(
                p.GranteeName ?? "—",
                p.GranteeIdNumber,
                p.Shares
            )).ToList();

            if (directShares == 0 && proxyShares == 0)
                continue;

            result.Add(new AttendanceListItemDto(
                sh.Id, sh.VsdcRow, sh.FullName, sh.IdNumber,
                attendanceMode,
                directShares, proxyShares, sh.VotingRights,
                delegations));
        }

        return result;
    }
}

public class GetAbsentShareholdersHandler
    : IRequestHandler<GetAbsentShareholdersQuery, IList<AbsentShareholderDto>>
{
    private readonly MmsDbContext _db;
    public GetAbsentShareholdersHandler(MmsDbContext db) => _db = db;

    public async Task<IList<AbsentShareholderDto>> Handle(
        GetAbsentShareholdersQuery query, CancellationToken ct)
    {
        var checkedInIds = await _db.AttendanceRecords
            .Where(a => a.MeetingId == query.MeetingId && a.IsActive)
            .Select(a => a.ShareholderId)
            .ToListAsync(ct);

        var activeStatuses = new[] { ProxyStatus.Pending, ProxyStatus.Confirmed };

        return await _db.Shareholders
            .Where(s => s.MeetingId == query.MeetingId && !checkedInIds.Contains(s.Id))
            .Select(s => new AbsentShareholderDto(
                s.Id, s.FullName, s.IdNumber, s.VotingRights,
                _db.Proxies.Any(p => p.MeetingId == query.MeetingId
                                  && p.GrantorId == s.Id
                                  && activeStatuses.Contains(p.Status)),
                _db.Proxies.Where(p => p.MeetingId == query.MeetingId
                                    && p.GrantorId == s.Id
                                    && activeStatuses.Contains(p.Status))
                           .Select(p => p.GranteeName).FirstOrDefault()))
            .ToListAsync(ct);
    }
}

public class CreateAttendanceSnapshotHandler
    : IRequestHandler<CreateAttendanceSnapshotCommand, Guid>
{
    private readonly MmsDbContext _db;
    private readonly IAuditLogService _audit;

    public CreateAttendanceSnapshotHandler(MmsDbContext db, IAuditLogService audit)
        => (_db, _audit) = (db, audit);

    public async Task<Guid> Handle(CreateAttendanceSnapshotCommand cmd, CancellationToken ct)
    {
        var meeting = await _db.Meetings.FindAsync(new object[] { cmd.MeetingId }, ct)
            ?? throw new InvalidOperationException("Cuộc họp không tồn tại.");

        var totalShares = meeting.TotalVotingShares;

        var activeAttendances = await _db.AttendanceRecords
            .Where(a => a.MeetingId == cmd.MeetingId && a.IsActive)
            .CountAsync(ct);

        var attendingShares = await _db.Ballots
            .Where(b => b.MeetingId == cmd.MeetingId
                     && b.AttendanceRecord != null && b.AttendanceRecord.IsActive
                     && b.BallotType == BallotType.VotingBallot
                     && b.Status != BallotStatus.Invalidated
                     && !b.IsSplitBallot)
            .SumAsync(b => b.VotingShares, ct);

        var quorum = totalShares > 0 ? (decimal)attendingShares / totalShares * 100m : 0;

        var snapshot = new AttendanceSnapshot
        {
            MeetingId = cmd.MeetingId,
            SnapshotType = cmd.SnapshotType,
            TotalAttendingShareholders = activeAttendances,
            TotalAttendingShares = attendingShares,
            PercentageQuorum = Math.Round(quorum, 4),
            SnapshotAt = DateTime.UtcNow,
            ConfirmedBy = cmd.ConfirmedByUserId,
        };

        _db.AttendanceSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(ct);

        try
        {
            await _audit.LogAsync(AuditCategory.CheckIn, "AttendanceSnapshot", snapshot.Id,
                $"Snapshot {cmd.SnapshotType}: {activeAttendances} CĐ, {attendingShares:N0} CP, {quorum:F2}%",
                cmd.ConfirmedByUserId, cmd.Actor, cmd.MeetingId, ct);
        }
        catch { }

        return snapshot.Id;
    }
}
