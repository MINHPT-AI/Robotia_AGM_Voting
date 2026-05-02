using Mms.Domain.Common;

namespace Mms.Domain.Entities;

/// <summary>
/// Nhóm phiếu tách — liên kết phiếu tách với cổ đông nguồn (BRD v2.3 Mục 5.3.2).
/// UNIQUE(AttendanceRecordId, SourceShareholderId) — mỗi CĐ nguồn chỉ xuất hiện trong 1 nhóm (RB-11).
/// </summary>
public class BallotGroup : BaseEntity
{
    public Guid AttendanceRecordId { get; set; }
    public AttendanceRecord AttendanceRecord { get; set; } = null!;

    public Guid BallotId { get; set; }
    public Ballot Ballot { get; set; } = null!;

    public Guid SourceShareholderId { get; set; }
    public Shareholder SourceShareholder { get; set; } = null!;

    public int GroupNumber { get; set; }                                  // Số thứ tự nhóm (1, 2, 3...)
    public Guid? CreatedBy { get; set; }                                  // OperatorUserId
}
