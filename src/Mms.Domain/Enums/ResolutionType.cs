namespace Mms.Domain.Enums;

/// <summary>
/// Loại nghị quyết — quyết định ngưỡng thông qua (BRD v2.3 Mục 1.2).
/// </summary>
public enum ResolutionType
{
    /// <summary>Nghị quyết thường: ngưỡng > 50%.</summary>
    Normal,
    /// <summary>Nghị quyết quan trọng: ngưỡng ≥ 65% (Điều 148 Luật DN 2020).</summary>
    Important
}
