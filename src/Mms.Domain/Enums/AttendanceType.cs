namespace Mms.Domain.Enums;

/// <summary>
/// Bốn tình huống tham dự tại check-in (BRD v2.3 Mục 5.6).
/// </summary>
public enum AttendanceType
{
    /// <summary>F1: Cổ đông trực tiếp toàn phần.</summary>
    F1_Direct,
    /// <summary>F2: Người nhận ủy quyền (không phải cổ đông hoặc đến với tư cách đại diện).</summary>
    F2_FullProxy,
    /// <summary>F3: Kết hợp — cổ đông giữ một phần, ủy quyền một phần.</summary>
    F3_Combined,
    /// <summary>F4: Người nhận ủy quyền đồng thời là cổ đông trực tiếp.</summary>
    F4_ProxyAndDirect
}
