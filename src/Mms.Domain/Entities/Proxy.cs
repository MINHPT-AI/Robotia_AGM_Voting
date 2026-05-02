using Mms.Domain.Common;
using Mms.Domain.Enums;

namespace Mms.Domain.Entities;

public class Proxy : BaseEntity
{
    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;
    public Guid GrantorId { get; set; }
    public Shareholder Grantor { get; set; } = null!;
    public string GranteeName { get; set; } = string.Empty;
    public string? GranteeIdNumber { get; set; }
    public long Shares { get; set; }
    public ProxyScope Scope { get; set; }
    public ProxyType ProxyType { get; set; }
    public DateOnly? ProxyDate { get; set; }
    public string? Detail { get; set; }
    public string? ScanUrl { get; set; }
    public DateTime? InvalidatedAt { get; set; }

    // ── Mở rộng theo BRD v2.3 ──

    /// <summary>Trạng thái vòng đời ủy quyền (UQ-04).</summary>
    public ProxyStatus Status { get; set; } = ProxyStatus.Pending;

    /// <summary>FK đến Shareholder nếu người nhận UQ là cổ đông trong VSDC.</summary>
    public Guid? GranteeShareholderId { get; set; }
    public Shareholder? GranteeShareholder { get; set; }

    /// <summary>FK đến ProxyRecipient nếu người nhận UQ không có trong VSDC.</summary>
    public Guid? GranteeRecipientId { get; set; }
    public ProxyRecipient? GranteeRecipient { get; set; }

    /// <summary>Self-ref khi ủy quyền này bị thay thế bởi bản mới.</summary>
    public Guid? SupersededById { get; set; }
    public Proxy? SupersededBy { get; set; }

    /// <summary>Lý do hủy (khi Status = Cancelled).</summary>
    public string? CancellationReason { get; set; }
}
