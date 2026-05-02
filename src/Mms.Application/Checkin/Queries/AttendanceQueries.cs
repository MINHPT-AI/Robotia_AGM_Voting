using MediatR;
using Mms.Domain.Enums;

namespace Mms.Application.Checkin.Queries;

/// <summary>Lấy danh sách DS1 (CĐ trực tiếp + đại diện UQ).</summary>
public record GetAttendanceListQuery(Guid MeetingId)
    : IRequest<IList<AttendanceListItemDto>>;

/// <summary>Lấy danh sách DS3 (CĐ vắng mặt).</summary>
public record GetAbsentShareholdersQuery(Guid MeetingId)
    : IRequest<IList<AbsentShareholderDto>>;

/// <summary>Chốt snapshot thẩm tra tư cách.</summary>
public record CreateAttendanceSnapshotCommand(
    Guid MeetingId,
    string SnapshotType,
    Guid ConfirmedByUserId,
    string Actor
) : IRequest<Guid>;

/// <summary>DS1 — Cổ đông dự họp (theo VSDC). Mỗi dòng = 1 cổ đông.</summary>
public record AttendanceListItemDto(
    Guid ShareholderId,
    string? VsdcRow,                    // STT VSDC
    string ShareholderName,
    string IdNumber,
    string AttendanceMode,              // "Trực tiếp" / "Ủy quyền" / "Trực tiếp + Ủy quyền"
    long DirectShares,                  // CP dự họp trực tiếp
    long ProxyShares,                   // CP đã ủy quyền cho người khác
    long TotalShares,                   // Tổng CP VSDC
    IList<ProxyDelegationDto> ProxyDelegations);  // Chi tiết ủy quyền cho ai

/// <summary>Thông tin ủy quyền đi — hiển thị khi click vào dòng DS1.</summary>
public record ProxyDelegationDto(
    string GranteeName,
    string? GranteeIdNumber,
    long Shares);

public record AbsentShareholderDto(
    Guid ShareholderId,
    string FullName,
    string IdNumber,
    long VotingRights,
    bool HasProxied,
    string? ProxiedToName);
