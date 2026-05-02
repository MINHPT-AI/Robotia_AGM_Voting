using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Common.Interfaces;
using Mms.Application.Proxies.Commands;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Proxies;

public class CreateProxyHandler : IRequestHandler<CreateProxyCommand, Guid>
{
    private readonly MmsDbContext _db;
    private readonly IAuditLogService _audit;

    public CreateProxyHandler(MmsDbContext db, IAuditLogService audit)
        => (_db, _audit) = (db, audit);

    public async Task<Guid> Handle(CreateProxyCommand cmd, CancellationToken ct)
    {
        // ── 1. Validate GrantorId exists ──
        var grantor = await _db.Shareholders
            .FirstOrDefaultAsync(s => s.Id == cmd.GrantorId && s.MeetingId == cmd.MeetingId, ct)
            ?? throw new InvalidOperationException("Cổ đông ủy quyền không tồn tại trong cuộc họp này.");

        // ── 2. Validate Meeting status ──
        var meeting = await _db.Meetings.FindAsync(new object[] { cmd.MeetingId }, ct)
            ?? throw new InvalidOperationException("Cuộc họp không tồn tại.");

        if (meeting.Status == MeetingStatus.Tallying || meeting.Status == MeetingStatus.Completed)
            throw new InvalidOperationException("RB-03: Không thể tạo ủy quyền sau khi đã bắt đầu kiểm phiếu.");

        // ── 3. Calculate available shares (UQ-01) ──
        var alreadyProxied = await _db.Proxies
            .Where(p => p.MeetingId == cmd.MeetingId
                     && p.GrantorId == cmd.GrantorId
                     && (p.Status == ProxyStatus.Pending || p.Status == ProxyStatus.Confirmed))
            .SumAsync(p => p.Shares, ct);

        var available = grantor.VotingRights - alreadyProxied;
        if (cmd.Shares > available)
            throw new InvalidOperationException(
                $"UQ-01: CP ủy quyền ({cmd.Shares:N0}) vượt quá CP khả dụng ({available:N0}).");

        if (cmd.Shares <= 0)
            throw new InvalidOperationException("Số cổ phần ủy quyền phải lớn hơn 0.");

        // ── 3b. Validate Full vs Partial scope ──
        if (cmd.Scope == ProxyScope.Full && cmd.Shares != available)
            throw new InvalidOperationException(
                $"UQ-02: Ủy quyền toàn phần phải ủy quyền toàn bộ CP khả dụng ({available:N0}), nhưng chỉ nhập {cmd.Shares:N0}.");

        if (cmd.Scope == ProxyScope.Partial && cmd.Shares >= available)
            throw new InvalidOperationException(
                $"UQ-02: Ủy quyền một phần phải nhỏ hơn CP khả dụng ({available:N0}). Nếu muốn ủy quyền toàn bộ, hãy chọn Toàn phần.");

        // ── 4. Validate 1-level proxy (RB-02) ──
        if (cmd.GranteeShareholderId.HasValue)
        {
            // Kiểm tra: người nhận UQ đã nhận CP từ người khác → không được UQ tiếp phần đó
            // Nhưng vẫn có thể UQ phần CP riêng của mình
            // RB-02 chỉ cấm "ủy quyền tiếp phần CP nhận từ người khác"
            // → Validate sẽ được kiểm tra khi người nhận tạo UQ đi (tại thời điểm đó)
        }

        // ── 5. Resolve grantee ──
        Guid? granteeShareholderId = cmd.GranteeShareholderId;
        Guid? granteeRecipientId = null;
        string granteeName;
        string? granteeIdNumber;

        if (cmd.GranteeShareholderId.HasValue)
        {
            // Người nhận là CĐ trong VSDC
            var grantee = await _db.Shareholders
                .FirstOrDefaultAsync(s => s.Id == cmd.GranteeShareholderId && s.MeetingId == cmd.MeetingId, ct)
                ?? throw new InvalidOperationException("Cổ đông nhận ủy quyền không tồn tại trong cuộc họp.");
            granteeName = grantee.FullName;
            granteeIdNumber = grantee.IdNumber;
        }
        else
        {
            // Người nhận ngoài VSDC (UQ-5) → lookup or create ProxyRecipient
            if (string.IsNullOrWhiteSpace(cmd.GranteeName) || string.IsNullOrWhiteSpace(cmd.GranteeIdNumber))
                throw new InvalidOperationException("UQ-03: Người nhận UQ ngoài VSDC phải có Họ tên và CMND/CCCD.");

            var existing = await _db.ProxyRecipients
                .FirstOrDefaultAsync(r => r.IdNumber == cmd.GranteeIdNumber, ct);

            if (existing != null)
            {
                granteeRecipientId = existing.Id;
                // Cập nhật SĐT nếu có
                if (!string.IsNullOrWhiteSpace(cmd.GranteePhoneNumber) && existing.PhoneNumber != cmd.GranteePhoneNumber)
                {
                    existing.PhoneNumber = cmd.GranteePhoneNumber;
                    existing.PhoneUpdatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                var recipient = new ProxyRecipient
                {
                    FullName = cmd.GranteeName,
                    IdNumber = cmd.GranteeIdNumber,
                    Organization = cmd.GranteeOrganization,
                    Position = cmd.GranteePosition,
                    PhoneNumber = cmd.GranteePhoneNumber,
                    PhoneUpdatedAt = !string.IsNullOrWhiteSpace(cmd.GranteePhoneNumber) ? DateTime.UtcNow : null,
                };
                _db.ProxyRecipients.Add(recipient);
                granteeRecipientId = recipient.Id;
            }

            granteeName = cmd.GranteeName;
            granteeIdNumber = cmd.GranteeIdNumber;
        }

        // ── 6. Create Proxy ──
        var proxy = new Proxy
        {
            MeetingId = cmd.MeetingId,
            GrantorId = cmd.GrantorId,
            GranteeName = granteeName,
            GranteeIdNumber = granteeIdNumber,
            Shares = cmd.Shares,
            Scope = cmd.Scope,
            ProxyType = cmd.ProxyType,
            ProxyDate = cmd.ProxyDate,
            Detail = cmd.Detail,
            Status = ProxyStatus.Pending,
            GranteeShareholderId = granteeShareholderId,
            GranteeRecipientId = granteeRecipientId,
        };

        _db.Proxies.Add(proxy);
        await _db.SaveChangesAsync(ct);

        // ── 7. Audit Log ──
        try
        {
            await _audit.LogAsync(AuditCategory.Proxy, nameof(Proxy), proxy.Id,
                $"Proxy created: {grantor.FullName} → {granteeName} ({cmd.Shares:N0} CP, {cmd.Scope})",
                cmd.OperatorUserId, cmd.Actor, cmd.MeetingId, ct);
        }
        catch { /* audit failure should not block save */ }

        return proxy.Id;
    }
}
