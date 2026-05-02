using Mms.Domain.Common;
using Mms.Domain.Enums;

namespace Mms.Domain.Entities;

/// <summary>
/// Phiên tham dự — ghi nhận sự hiện diện của 1 cổ đông tại cuộc họp (BRD v2.3 Mục 5).
/// Mỗi cổ đông chỉ có tối đa 1 Phiên tham dự ACTIVE tại 1 cuộc họp (RB-04).
/// </summary>
public class AttendanceRecord : BaseEntity
{
    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;

    public Guid ShareholderId { get; set; }
    public Shareholder Shareholder { get; set; } = null!;

    // Thông tin người vật lý tại quầy
    public string PhysicalAttendeeIdNumber { get; set; } = string.Empty;  // CMND/CCCD/Passport
    public string PhysicalAttendeeName { get; set; } = string.Empty;

    public AttendanceType AttendanceType { get; set; }
    public string AttendCode { get; set; } = string.Empty;               // [Ticker]-[Year]-[00001]

    // Số điện thoại (BRD v2.3 Mục 5.4)
    public string? PhoneNumber { get; set; }
    public PhoneSource? PhoneSource { get; set; }

    // Quà tặng (BRD v2.3 Mục 5.5)
    public bool GiftReceived { get; set; }
    public DateTime? GiftReceivedAt { get; set; }
    public Guid? GiftReceivedBy { get; set; }                            // OperatorUserId

    // Check-in metadata
    public DateTime CheckedInAt { get; set; }
    public string? PosTerminal { get; set; }
    public Guid? OperatorUserId { get; set; }

    // Status
    public bool IsActive { get; set; } = true;
    public string? CancelReason { get; set; }

    // Optimistic concurrency
    public uint Xmin { get; set; }

    // Navigation
    public ICollection<Ballot> Ballots { get; set; } = new List<Ballot>();
    public ICollection<BallotGroup> BallotGroups { get; set; } = new List<BallotGroup>();
}
