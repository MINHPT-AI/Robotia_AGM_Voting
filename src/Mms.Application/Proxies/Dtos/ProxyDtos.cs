using Mms.Domain.Enums;

namespace Mms.Application.Proxies.Dtos;

/// <summary>Thông tin ủy quyền hiển thị trên danh sách.</summary>
public record ProxyListItemDto(
    Guid Id,
    Guid GrantorId,
    string GrantorName,
    string GrantorIdNumber,
    long GrantorTotalShares,
    string GranteeName,
    string? GranteeIdNumber,
    long Shares,
    ProxyScope Scope,
    ProxyType ProxyType,
    ProxyStatus Status,
    DateOnly? ProxyDate,
    DateTime CreatedAt);

/// <summary>Thông tin cổ đông + trạng thái ủy quyền tại SC-01.</summary>
public record ShareholderProxyStatusDto(
    Guid ShareholderId,
    string FullName,
    string IdNumber,
    long TotalShares,
    long SharesAlreadyProxied,
    long AvailableShares,
    bool IsReceivingProxy,
    long ReceivedProxyShares,
    IList<ProxyListItemDto> OutgoingProxies,
    IList<ProxyListItemDto> IncomingProxies);

/// <summary>Thông tin người nhận UQ không có trong VSDC.</summary>
public record ProxyRecipientDto(
    Guid Id,
    string FullName,
    string IdNumber,
    string? Organization,
    string? Position,
    string? PhoneNumber);

/// <summary>Topbar dòng 2 — thống kê ủy quyền real-time.</summary>
public record ProxyTopbarDto(
    int TotalShareholdersWithProxy,
    long TotalProxiedShares,
    int PendingCount);

/// <summary>Nhóm theo người nhận UQ — tổng hợp ai đang nhận bao nhiêu CP từ bao nhiêu CĐ.</summary>
public record GranteeGroupDto(
    string GranteeName,
    string? GranteeIdNumber,
    int GrantorCount,
    long TotalShares,
    int ConfirmedCount,
    int PendingCount,
    IList<ProxyListItemDto> Proxies);

/// <summary>Nhóm theo người ủy quyền — tổng hợp CĐ đã ủy quyền cho ai, bao nhiêu CP.</summary>
public record GrantorGroupDto(
    Guid GrantorId,
    string GrantorName,
    string GrantorIdNumber,
    long TotalShares,
    long SharesProxied,
    long AvailableShares,
    int GranteeCount,
    int ConfirmedCount,
    int PendingCount,
    IList<ProxyListItemDto> Proxies);
