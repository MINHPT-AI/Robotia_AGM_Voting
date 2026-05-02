namespace Mms.Domain.Enums;

/// <summary>
/// Phân loại phiếu biểu quyết (BRD v2.3 Mục 7.2).
/// </summary>
public enum VoteChoice
{
    /// <summary>Hợp lệ — Tán thành (vào mẫu số + tử số).</summary>
    Approve,
    /// <summary>Hợp lệ — Không tán thành (vào mẫu số, không vào tử số).</summary>
    Reject,
    /// <summary>Hợp lệ — Ý kiến khác (vào mẫu số, không vào tử số).</summary>
    Abstain,
    /// <summary>Không hợp lệ (vào mẫu số, không vào tử số).</summary>
    Invalid
}
