using Mms.Domain.Common;
using Mms.Domain.Enums;

namespace Mms.Domain.Entities;

public class MeetingResolution : BaseEntity
{
    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }

    // ── Mở rộng theo BRD v2.3 ──

    /// <summary>Loại nghị quyết: Thường (>50%) hoặc Quan trọng (≥65%).</summary>
    public ResolutionType ResolutionType { get; set; } = ResolutionType.Normal;

    /// <summary>Ngưỡng thông qua tùy chỉnh theo Điều lệ công ty (BRD v2.3 Mục 1.2).</summary>
    public decimal ApprovalThreshold { get; set; } = 0.50m;
}
