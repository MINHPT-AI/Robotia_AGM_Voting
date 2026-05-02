using MediatR;
using Mms.Domain.Enums;

namespace Mms.Application.Proxies.Commands;

/// <summary>
/// Tạo ủy quyền mới (UQ-1 đến UQ-5).
/// </summary>
public record CreateProxyCommand(
    Guid MeetingId,
    Guid GrantorId,
    long Shares,
    ProxyScope Scope,
    ProxyType ProxyType,
    DateOnly? ProxyDate,
    string? Detail,
    // Người nhận — chỉ 1 trong 2 trường được điền
    Guid? GranteeShareholderId,       // Nếu người nhận là CĐ trong VSDC
    // Nếu người nhận ngoài VSDC — tạo hoặc tra cứu ProxyRecipient
    string? GranteeName,
    string? GranteeIdNumber,
    string? GranteeOrganization,
    string? GranteePosition,
    string? GranteePhoneNumber,
    // Metadata
    string Actor,
    Guid? OperatorUserId
) : IRequest<Guid>;

/// <summary>
/// Hủy ủy quyền (UQ-4).
/// </summary>
public record CancelProxyCommand(
    Guid ProxyId,
    string CancellationReason,
    string Actor,
    Guid? OperatorUserId
) : IRequest;

/// <summary>
/// Duyệt ủy quyền.
/// </summary>
public record ConfirmProxyCommand(
    Guid ProxyId,
    string Actor,
    Guid? OperatorUserId
) : IRequest;

/// <summary>
/// Import hàng loạt ủy quyền từ file Excel.
/// </summary>
public record ImportProxiesCommand(
    Guid MeetingId,
    IList<ProxyImportRowDto> Rows,
    string Actor,
    Guid? OperatorUserId
) : IRequest<ProxyImportResultDto>;

/// <summary>Một dòng import ủy quyền đã validate sơ bộ từ Excel.</summary>
public record ProxyImportRowDto(
    int RowNumber,
    int Stt,
    string GrantorName,
    string GrantorIdNumber,
    string GranteeName,
    string GranteeIdNumber,
    long Shares,
    string? GranteePhone);

/// <summary>Kết quả validate 1 dòng import.</summary>
public record ProxyImportValidationRow(
    int RowNumber,
    int Stt,
    string GrantorName,
    string GrantorIdNumber,
    string GranteeName,
    string GranteeIdNumber,
    long Shares,
    string? GranteePhone,
    bool IsValid,
    string? ErrorMessage,
    long AvailableShares);

/// <summary>Kết quả tổng hợp import ủy quyền.</summary>
public record ProxyImportResultDto(
    int TotalRows,
    int SuccessCount,
    int ErrorCount,
    IList<ProxyImportValidationRow> Rows);
