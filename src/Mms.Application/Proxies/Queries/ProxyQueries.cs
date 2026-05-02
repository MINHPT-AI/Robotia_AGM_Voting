using MediatR;
using Mms.Application.Proxies.Commands;
using Mms.Application.Proxies.Dtos;

namespace Mms.Application.Proxies.Queries;

/// <summary>Lấy trạng thái ủy quyền đầy đủ của 1 cổ đông (cho panel trái SC-01).</summary>
public record GetShareholderProxyStatusQuery(Guid MeetingId, Guid ShareholderId)
    : IRequest<ShareholderProxyStatusDto?>;

/// <summary>Lấy danh sách toàn bộ ủy quyền của cuộc họp (cho Drawer SC-01).</summary>
public record GetAllProxiesQuery(Guid MeetingId)
    : IRequest<IList<ProxyListItemDto>>;

/// <summary>Lấy thống kê topbar ủy quyền.</summary>
public record GetProxyTopbarQuery(Guid MeetingId)
    : IRequest<ProxyTopbarDto>;

/// <summary>Tra cứu danh sách proxy_recipients theo IdNumber (autocomplete).</summary>
public record SearchProxyRecipientsQuery(string IdNumberPrefix)
    : IRequest<IList<ProxyRecipientDto>>;

/// <summary>Tính số cổ phần khả dụng để ủy quyền.</summary>
public record GetAvailableSharesQuery(Guid MeetingId, Guid ShareholderId)
    : IRequest<long>;

/// <summary>Lấy danh sách người nhận UQ, gom nhóm theo tên + CCCD.</summary>
public record GetGranteeGroupsQuery(Guid MeetingId, string? SearchText = null)
    : IRequest<IList<GranteeGroupDto>>;

/// <summary>Lấy danh sách người ủy quyền (CĐ đi UQ), gom nhóm theo CĐ.</summary>
public record GetGrantorGroupsQuery(Guid MeetingId, string? SearchText = null)
    : IRequest<IList<GrantorGroupDto>>;

/// <summary>Validate import ủy quyền (dry-run, không lưu DB).</summary>
public record ValidateProxiesImportQuery(
    Guid MeetingId,
    IList<ProxyImportRowDto> Rows
) : IRequest<ProxyImportResultDto>;
