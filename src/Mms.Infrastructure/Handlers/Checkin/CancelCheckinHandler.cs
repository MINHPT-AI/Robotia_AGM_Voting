using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Checkin.Commands;
using Mms.Application.Common.Interfaces;
using Mms.Domain.Enums;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Checkin;

public class CancelCheckinHandler : IRequestHandler<CancelCheckinCommand>
{
    private readonly MmsDbContext _db;
    private readonly IAuditLogService _audit;

    public CancelCheckinHandler(MmsDbContext db, IAuditLogService audit)
        => (_db, _audit) = (db, audit);

    public async Task Handle(CancelCheckinCommand cmd, CancellationToken ct)
    {
        var attendance = await _db.AttendanceRecords
            .Include(a => a.Ballots)
            .Include(a => a.Shareholder)
            .FirstOrDefaultAsync(a => a.Id == cmd.AttendanceRecordId, ct)
            ?? throw new InvalidOperationException("Phiên tham dự không tồn tại.");

        if (!attendance.IsActive)
            throw new InvalidOperationException("Phiên tham dự đã bị hủy trước đó.");

        // ── RB-05: Invalidate all ballots before deactivating attendance ──
        foreach (var ballot in attendance.Ballots.Where(b => b.Status != BallotStatus.Invalidated))
        {
            ballot.Status = BallotStatus.Invalidated;
            ballot.InvalidationReason = $"Rút lui: {cmd.CancelReason}";
            ballot.InvalidatedAt = DateTime.UtcNow;
        }

        attendance.IsActive = false;
        attendance.CancelReason = cmd.CancelReason;
        attendance.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        try
        {
            await _audit.LogAsync(AuditCategory.CheckIn, "AttendanceRecord", attendance.Id,
                $"Check-in cancelled (rút lui): {attendance.Shareholder.FullName}. Lý do: {cmd.CancelReason}",
                cmd.OperatorUserId, cmd.Actor, attendance.MeetingId, ct);
        }
        catch { }
    }
}

public class ToggleGiftHandler : IRequestHandler<ToggleGiftCommand>
{
    private readonly MmsDbContext _db;
    private readonly IAuditLogService _audit;

    public ToggleGiftHandler(MmsDbContext db, IAuditLogService audit)
        => (_db, _audit) = (db, audit);

    public async Task Handle(ToggleGiftCommand cmd, CancellationToken ct)
    {
        var attendance = await _db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.Id == cmd.AttendanceRecordId, ct)
            ?? throw new InvalidOperationException("Phiên tham dự không tồn tại.");

        attendance.GiftReceived = cmd.GiftReceived;
        attendance.GiftReceivedAt = cmd.GiftReceived ? DateTime.UtcNow : null;
        attendance.GiftReceivedBy = cmd.GiftReceived ? cmd.OperatorUserId : null;
        attendance.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        try
        {
            await _audit.LogAsync(AuditCategory.Gift, "AttendanceRecord", attendance.Id,
                $"Gift {(cmd.GiftReceived ? "received" : "unchecked")}",
                cmd.OperatorUserId, cmd.Actor, attendance.MeetingId, ct);
        }
        catch { }
    }
}
