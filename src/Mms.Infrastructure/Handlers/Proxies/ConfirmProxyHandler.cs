using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Common.Interfaces;
using Mms.Application.Proxies.Commands;
using Mms.Domain.Enums;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Proxies;

public class ConfirmProxyHandler : IRequestHandler<ConfirmProxyCommand>
{
    private readonly MmsDbContext _db;
    private readonly IAuditLogService _audit;

    public ConfirmProxyHandler(MmsDbContext db, IAuditLogService audit)
        => (_db, _audit) = (db, audit);

    public async Task Handle(ConfirmProxyCommand cmd, CancellationToken ct)
    {
        var proxy = await _db.Proxies
            .FirstOrDefaultAsync(p => p.Id == cmd.ProxyId, ct)
            ?? throw new InvalidOperationException("Ủy quyền không tồn tại.");

        if (proxy.Status != ProxyStatus.Pending)
            throw new InvalidOperationException($"Không thể duyệt ủy quyền ở trạng thái {proxy.Status}.");

        // Validate Meeting
        var meeting = await _db.Meetings.FindAsync(new object[] { proxy.MeetingId }, ct)
            ?? throw new InvalidOperationException("Cuộc họp không tồn tại.");

        if (meeting.Status == MeetingStatus.Tallying || meeting.Status == MeetingStatus.Completed)
            throw new InvalidOperationException("Không thể duyệt ủy quyền sau khi đã bắt đầu kiểm phiếu.");

        proxy.Status = ProxyStatus.Confirmed;

        await _db.SaveChangesAsync(ct);

        try
        {
            await _audit.LogAsync(AuditCategory.Proxy, nameof(Mms.Domain.Entities.Proxy), proxy.Id,
                $"Proxy confirmed: {proxy.Shares:N0} CP",
                cmd.OperatorUserId, cmd.Actor, proxy.MeetingId, ct);
        }
        catch { /* audit failure should not block save */ }
    }
}
