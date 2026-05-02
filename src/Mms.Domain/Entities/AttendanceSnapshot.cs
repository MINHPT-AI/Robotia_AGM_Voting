using Mms.Domain.Common;

namespace Mms.Domain.Entities;

/// <summary>
/// Snapshot chốt thẩm tra tư cách tại 2 thời điểm (BRD v2.3 Mục 6.1).
/// </summary>
public class AttendanceSnapshot : BaseEntity
{
    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;

    /// <summary>"Opening" = Chốt lần 1 (khai mạc), "PreVote" = Chốt lần 2 (trước biểu quyết).</summary>
    public string SnapshotType { get; set; } = string.Empty;

    public int TotalAttendingShareholders { get; set; }
    public long TotalAttendingShares { get; set; }
    public decimal PercentageQuorum { get; set; }

    public int TotalPhysicalAttendees { get; set; }
    public int TotalBallotsIssued { get; set; }

    public DateTime SnapshotAt { get; set; }
    public Guid ConfirmedBy { get; set; }
    public string? Notes { get; set; }
}
