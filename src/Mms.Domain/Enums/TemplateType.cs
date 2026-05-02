namespace Mms.Domain.Enums;

/// <summary>
/// 7 loại template theo BRD v2.3 Mục 6.2.
/// </summary>
public enum TemplateType
{
    /// <summary>Loại 1: Thư mời.</summary>
    Invitation,
    /// <summary>Loại 2: Thẻ biểu quyết.</summary>
    VotingCard,
    /// <summary>Loại 3: Phiếu biểu quyết.</summary>
    VotingBallot,
    /// <summary>Loại 4a: Phiếu bầu TVHĐQT.</summary>
    ElectionHDQT,
    /// <summary>Loại 4b: Phiếu bầu BKS.</summary>
    ElectionBKS,
    /// <summary>Loại 5: Biên bản Thẩm tra Tư cách.</summary>
    QuorumReport,
    /// <summary>Loại 6: Biên bản Kiểm phiếu.</summary>
    TallyReport
}
