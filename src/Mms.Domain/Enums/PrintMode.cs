namespace Mms.Domain.Enums;

/// <summary>
/// Ba chế độ in phiếu biểu quyết (BRD v2.3 Mục 5.3.1).
/// </summary>
public enum PrintMode
{
    /// <summary>IN-1: Gộp — mỗi người 1 phiếu tổng hợp tất cả CP.</summary>
    Consolidated,
    /// <summary>IN-2: Tách theo nguồn — mỗi cổ đông nguồn 1 phiếu.</summary>
    SplitBySource,
    /// <summary>IN-3: Hybrid — mặc định gộp, override tại quầy.</summary>
    Hybrid
}
