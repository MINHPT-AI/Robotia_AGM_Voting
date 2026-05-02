using Mms.Domain.Common;
using Mms.Domain.Enums;

namespace Mms.Domain.Entities;

public class MeetingCandidate : BaseEntity
{
    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty; // HDQT / BKS
    public int? BirthYear { get; set; }
    public string? Notes { get; set; }
    public string? CurrentPosition { get; set; }  // Chức vụ hiện tại

    // ── Mở rộng theo BRD v2.3 ──

    /// <summary>Bảng bầu cử: HĐQT hoặc BKS — xác định phiếu bầu nào chứa ứng viên này.</summary>
    public CandidateBoard CandidateBoard { get; set; }

    /// <summary>Số ghế cần bầu cho bảng này (dùng tính Cumulative Voting).</summary>
    public int NumberOfSeats { get; set; }
}
