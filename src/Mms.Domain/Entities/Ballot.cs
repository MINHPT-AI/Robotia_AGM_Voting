using Mms.Domain.Common;
using Mms.Domain.Enums;

namespace Mms.Domain.Entities;

public class Ballot : BaseEntity
{
    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;
    public Guid ShareholderId { get; set; }
    public Shareholder Shareholder { get; set; } = null!;
    public string AttendCode { get; set; } = string.Empty;
    public long VotingShares { get; set; }
    public long DirectShares { get; set; }
    public long ProxyShares { get; set; }
    public BallotStatus Status { get; set; } = BallotStatus.PendingPrint;
    public Guid? ParentBallotId { get; set; }
    public Ballot? ParentBallot { get; set; }
    public bool ReprintNeeded { get; set; }
    public string? InvalidationReason { get; set; }
    public string? PosTerminal { get; set; }
    public Guid? OperatorUserId { get; set; }
    public DateTime? InvalidatedAt { get; set; }
    public DateTime? PrintedAt { get; set; }

    // ── Mở rộng theo BRD v2.3 ──

    /// <summary>FK đến Phiên tham dự.</summary>
    public Guid? AttendanceRecordId { get; set; }
    public AttendanceRecord? AttendanceRecord { get; set; }

    /// <summary>Loại phiếu: Thẻ BQ, Phiếu BQ, Bầu HĐQT, Bầu BKS (BRD v2.3 Mục 5.3.0).</summary>
    public BallotType BallotType { get; set; } = BallotType.VotingBallot;

    /// <summary>Hậu tố khi tách phiếu: -1, -2, -3... (BRD v2.3 Mục 5.3.2).</summary>
    public int? SplitSequence { get; set; }

    /// <summary>Phiếu này có phải phiếu tách không.</summary>
    public bool IsSplitBallot { get; set; }

    /// <summary>Đã được duyệt nhanh (Bulk Approve).</summary>
    public bool BulkApproved { get; set; }

    /// <summary>Ghi chú đại diện UQ trên phiếu (VD: "đại diện UQ: Quỹ A, Quỹ B").</summary>
    public string? ProxyRepresentationNote { get; set; }

    // Npgsql optimistic concurrency via Postgres xmin system column
    public uint Xmin { get; set; }

    // Navigation
    public ICollection<VoteResult> VoteResults { get; set; } = new List<VoteResult>();
    public ICollection<ElectionVote> ElectionVotes { get; set; } = new List<ElectionVote>();
}
