using MediatR;
using Mms.Application.Checkin.Dtos;
using Mms.Domain.Enums;

namespace Mms.Application.Checkin.Commands;

/// <summary>Xác nhận check-in: tạo AttendanceRecord + phát phiếu.</summary>
public record PerformCheckinCommand(
    Guid MeetingId,
    Guid ShareholderId,
    string PhysicalAttendeeIdNumber,
    string PhysicalAttendeeName,
    string? PhoneNumber,
    PrintMode PrintMode,
    bool GiftReceived,
    string? PosTerminal,
    string Actor,
    Guid? OperatorUserId
) : IRequest<CheckinResultDto>;

/// <summary>Cấu hình tách phiếu (SPLIT).</summary>
public record ConfigureSplitBallotCommand(
    Guid AttendanceRecordId,
    IList<SplitGroup> Groups,
    string Actor,
    Guid? OperatorUserId
) : IRequest;

/// <summary>Nhóm tách phiếu: danh sách CĐ nguồn thuộc nhóm này.</summary>
public record SplitGroup(int GroupNumber, IList<Guid> SourceShareholderIds);

/// <summary>Hủy check-in (rút lui).</summary>
public record CancelCheckinCommand(
    Guid AttendanceRecordId,
    string CancelReason,
    string Actor,
    Guid? OperatorUserId
) : IRequest;

/// <summary>Tick nhận quà.</summary>
public record ToggleGiftCommand(
    Guid AttendanceRecordId,
    bool GiftReceived,
    string Actor,
    Guid? OperatorUserId
) : IRequest;

/// <summary>MERGE 2 tài khoản trùng ĐKSH tại quầy (BRD v2.3 Mục 2.3).</summary>
public record MergeAtCounterCommand(
    Guid MeetingId,
    Guid ShareholderId1,
    Guid ShareholderId2,
    Guid ConfirmedByUser1,    // Trưởng quầy
    Guid ConfirmedByUser2,    // Thành viên thứ 2
    string? CccdImagePath,
    string Actor
) : IRequest<Guid>;  // Returns surviving ShareholderId
