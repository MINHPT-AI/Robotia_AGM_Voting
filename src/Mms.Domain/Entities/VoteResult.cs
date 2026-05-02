using Mms.Domain.Common;
using Mms.Domain.Enums;

namespace Mms.Domain.Entities;

/// <summary>
/// Kết quả biểu quyết từng nội dung trên 1 phiếu (BRD v2.3 Mục 7).
/// </summary>
public class VoteResult : BaseEntity
{
    public Guid BallotId { get; set; }
    public Ballot Ballot { get; set; } = null!;

    public Guid MeetingResolutionId { get; set; }
    public MeetingResolution MeetingResolution { get; set; } = null!;

    public VoteChoice VoteChoice { get; set; } = VoteChoice.Approve;     // Mặc định Tán thành
    public long VotingShares { get; set; }                                // CP đại diện bởi phiếu này
    public bool BulkApproved { get; set; }                                // Đã duyệt nhanh

    public Guid? EnteredBy { get; set; }
    public DateTime EnteredAt { get; set; }
}
