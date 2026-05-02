using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Common.Interfaces;
using Mms.Application.Proxies.Commands;
using Mms.Domain.Enums;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Proxies;

public class CancelProxyHandler : IRequestHandler<CancelProxyCommand>
{
    private readonly MmsDbContext _db;
    private readonly IAuditLogService _audit;

    public CancelProxyHandler(MmsDbContext db, IAuditLogService audit)
        => (_db, _audit) = (db, audit);

    public async Task Handle(CancelProxyCommand cmd, CancellationToken ct)
    {
        var proxy = await _db.Proxies
            .Include(p => p.Grantor)
            .FirstOrDefaultAsync(p => p.Id == cmd.ProxyId, ct)
            ?? throw new InvalidOperationException("Ủy quyền không tồn tại.");

        if (proxy.Status == ProxyStatus.Cancelled || proxy.Status == ProxyStatus.Superseded)
            throw new InvalidOperationException("Ủy quyền này đã bị hủy hoặc thay thế.");

        // Check meeting status
        var meeting = await _db.Meetings.FindAsync(new object[] { proxy.MeetingId }, ct)
            ?? throw new InvalidOperationException("Cuộc họp không tồn tại.");

        if (meeting.Status == MeetingStatus.Tallying || meeting.Status == MeetingStatus.Completed)
            throw new InvalidOperationException("RB-03: Không thể hủy ủy quyền sau khi đã bắt đầu kiểm phiếu.");

        // Cancel proxy
        proxy.Status = ProxyStatus.Cancelled;
        proxy.CancellationReason = cmd.CancellationReason;
        proxy.InvalidatedAt = DateTime.UtcNow;
        proxy.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // NOTE: Ballot Lifecycle (L1, L2, L6) sẽ được xử lý bởi BallotLifecycleService
        // trong Phase 2 — tại đây chỉ hủy bản ghi ủy quyền.

        try
        {
            await _audit.LogAsync(AuditCategory.Proxy, "Proxy", proxy.Id,
                $"Proxy cancelled: {proxy.Grantor.FullName} → {proxy.GranteeName} ({proxy.Shares:N0} CP). Lý do: {cmd.CancellationReason}",
                cmd.OperatorUserId, cmd.Actor, proxy.MeetingId, ct);
        }
        catch { }
    }
}
