namespace Mms.Domain.Enums;

/// <summary>
/// Nguồn số điện thoại theo thứ tự ưu tiên (BRD v2.3 Mục 5.4).
/// </summary>
public enum PhoneSource
{
    /// <summary>Cột 9 file VSDC — tự động lấy.</summary>
    VSDC,
    /// <summary>Bảng proxy_recipients — người nhận UQ đã nhập trước đó.</summary>
    ProxyRecipient,
    /// <summary>Nhập thủ công tại quầy check-in.</summary>
    Manual
}
