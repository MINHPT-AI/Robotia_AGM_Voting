using Mms.Domain.Common;

namespace Mms.Domain.Entities;

/// <summary>
/// Kết quả bầu cử nhân sự trên 1 phiếu (BRD v2.3 Mục 7.4 — Cumulative Voting).
/// </summary>
public class ElectionVote : BaseEntity
{
    public Guid BallotId { get; set; }
    public Ballot Ballot { get; set; } = null!;

    public Guid MeetingCandidateId { get; set; }
    public MeetingCandidate MeetingCandidate { get; set; } = null!;

    public long Points { get; set; }                                      // Điểm bầu cho ứng viên này

    public Guid? EnteredBy { get; set; }
    public DateTime EnteredAt { get; set; }
}
