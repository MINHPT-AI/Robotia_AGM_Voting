using Mms.Domain.Enums;

namespace Mms.Application.Checkin.Dtos;

/// <summary>Topbar — cập nhật real-time qua polling / SignalR.</summary>
public record CheckinTopbarDto(
    // Dòng 1 — Số cổ đông
    int TotalVsdcShareholders,          // DS VSDC — cố định
    int AttendingShareholders,          // CĐ Dự họp (DS1: trực tiếp + ủy quyền đã CI)
    // Dòng 2 — Số CP biểu quyết
    long TotalVsdcShares,               // DS VSDC — cố định
    long AttendingShares,               // CP Dự họp
    // Tỷ lệ + trạng thái
    decimal QuorumPercentage,
    bool IsQuorumReached,
    // Dòng 3 — DS2 phụ
    int PhysicalAttendees,              // Người trực tiếp (theo ĐKSH duy nhất)
    int TotalBallotsIssued);

/// <summary>Kết quả tra cứu / phân tích tình huống tại quầy.</summary>
public record CheckinSituationDto(
    /// <summary>Mã tình huống: F1, F2, F3, F4, MERGE, ALREADY_CHECKED_IN, NOT_FOUND.</summary>
    string SituationCode,
    string SituationLabel,
    string? WarningMessage,
    Guid ShareholderId,
    string ShareholderName,
    string ShareholderIdNumber,
    long DirectShares,
    long ProxyReceivedShares,
    long TotalRepresentingShares,
    AttendanceType AttendanceType,
    // Proxy details
    IList<ProxyInfoDto> IncomingProxies,
    // Phone
    string? PhoneNumber,
    PhoneSource? PhoneSource,
    // Merge
    bool HasDuplicateDksh,
    DuplicateDkshDto? DuplicateInfo,
    // Already checked in?
    bool AlreadyCheckedIn,
    Guid? ExistingAttendanceRecordId,
    // VSDC STT
    string? VsdcRow);

/// <summary>Thông tin proxy nhận được — hiển thị trên thẻ CĐ.</summary>
public record ProxyInfoDto(
    Guid ProxyId,
    string GrantorName,
    long Shares,
    ProxyStatus Status);

/// <summary>Thông tin 2 tài khoản trùng ĐKSH (BRD v2.3 Mục 2.3).</summary>
public record DuplicateDkshDto(
    Guid ShareholderId1,
    string Name1,
    DateOnly? IdIssueDate1,
    long Shares1,
    Guid ShareholderId2,
    string Name2,
    DateOnly? IdIssueDate2,
    long Shares2);

/// <summary>Kết quả check-in — trả về sau khi xác nhận.</summary>
public record CheckinResultDto(
    Guid AttendanceRecordId,
    string AttendCode,
    AttendanceType AttendanceType,
    long TotalShares,
    IList<BallotIssuedDto> BallotsIssued);

/// <summary>Phiếu đã phát.</summary>
public record BallotIssuedDto(
    Guid BallotId,
    BallotType BallotType,
    string AttendCode,
    long VotingShares,
    int? SplitSequence,
    BallotStatus Status);

/// <summary>Phiếu trong hàng đợi in lại.</summary>
public record ReprintQueueItemDto(
    Guid BallotId,
    BallotType BallotType,
    string AttendCode,
    string ShareholderName,
    string Reason,
    DateTime InvalidatedAt);
