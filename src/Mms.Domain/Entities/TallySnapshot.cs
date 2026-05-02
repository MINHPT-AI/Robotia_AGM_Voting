using Mms.Domain.Common;

namespace Mms.Domain.Entities;

/// <summary>
/// Snapshot chốt kết quả kiểm phiếu — bất biến sau khi lock (BRD v2.3 Mục 7.7).
/// Yêu cầu xác nhận kép (2 thành viên Ban kiểm phiếu).
/// </summary>
public class TallySnapshot : BaseEntity
{
    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;

    /// <summary>"Interim" hoặc "Final".</summary>
    public string SnapshotType { get; set; } = string.Empty;

    public int TotalBallotIssued { get; set; }
    public int TotalBallotCounted { get; set; }
    public int TotalBallotNotReturned { get; set; }
    public int TotalBallotInvalid { get; set; }
    public long DenominatorShares { get; set; }                           // Mẫu số NQ

    public Guid ConfirmedBy1 { get; set; }
    public Guid ConfirmedBy2 { get; set; }
    public DateTime? LockedAt { get; set; }
}
