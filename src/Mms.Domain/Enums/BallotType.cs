namespace Mms.Domain.Enums;

/// <summary>
/// Bốn loại thẻ/phiếu phát tại check-in (BRD v2.3 Mục 5.3.0).
/// </summary>
public enum BallotType
{
    /// <summary>Template 2: Thẻ biểu quyết — thẻ định danh tham dự.</summary>
    VotingCard,
    /// <summary>Template 3: Phiếu biểu quyết — bỏ phiếu các nội dung tờ trình.</summary>
    VotingBallot,
    /// <summary>Template 4a: Phiếu bầu TVHĐQT.</summary>
    ElectionHDQT,
    /// <summary>Template 4b: Phiếu bầu BKS.</summary>
    ElectionBKS
}
