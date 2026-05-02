using Mms.Domain.Common;
using Mms.Domain.Enums;

namespace Mms.Domain.Entities;

/// <summary>
/// Gán template + cấu hình mã (QR/Barcode) cho từng loại phiếu tại cuộc họp cụ thể (BRD v2.3 Mục 6.2).
/// </summary>
public class MeetingTemplateConfig : BaseEntity
{
    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;

    public TemplateType TemplateType { get; set; }              // Loại phiếu (VotingCard, VotingBallot, ElectionHDQT, ElectionBKS...)
    public Guid TemplateId { get; set; }                         // FK đến Template
    public Template Template { get; set; } = null!;
    public CodeType CodeType { get; set; }                       // QR hoặc Barcode
}
