using Mms.Domain.Common;

namespace Mms.Domain.Entities;

/// <summary>
/// Người nhận ủy quyền không có trong danh sách VSDC (BRD v2.3 Mục 4.1 UQ-03).
/// Lưu trữ lâu dài để tái sử dụng qua các cuộc họp — đặc biệt trường SĐT.
/// </summary>
public class ProxyRecipient : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;        // CMND/CCCD/Passport
    public string? Organization { get; set; }                    // Đơn vị, tổ chức
    public string? Position { get; set; }                        // Chức vụ
    public string? PhoneNumber { get; set; }                     // Khuyến nghị mạnh — phục vụ thu hồi phiếu
    public DateTime? PhoneUpdatedAt { get; set; }
}
