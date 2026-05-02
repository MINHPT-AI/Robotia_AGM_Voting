namespace Mms.Domain.Enums;

/// <summary>
/// Vòng đời phiếu biểu quyết (BRD v2.3 Mục 7.1).
/// </summary>
public enum BallotStatus
{
    /// <summary>Đã tạo, chờ in.</summary>
    PendingPrint,
    /// <summary>Đã in và phát — đang lưu hành.</summary>
    Active,
    /// <summary>Đã bị hủy (do Ballot Lifecycle L1-L8).</summary>
    Invalidated,
    /// <summary>Đã thu về và nhập kết quả.</summary>
    Counted,
    /// <summary>Không thu về — loại khỏi mẫu số NQ.</summary>
    NotReturned
}
