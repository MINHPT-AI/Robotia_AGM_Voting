using Mms.Domain.Common;

namespace Mms.Domain.Entities;

public class Shareholder : BaseEntity
{
    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;

    // VSDC 16-column import fields
    public string VsdcRow { get; set; } = string.Empty;       // "1.1", "2.3" — STT gốc từ VSDC
    public int DisplayOrder { get; set; }                      // 1, 2, 3… để sort ổn định
    public string FullName { get; set; } = string.Empty;
    public string? Sid { get; set; }
    public string? InvestorCode { get; set; }
    public string IdNumber { get; set; } = string.Empty;       // Col 5 — mandatory, unique per meeting
    public DateOnly? IdIssueDate { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Nationality { get; set; }                    // Col 10
    public long SharesNonDeposit { get; set; }
    public long SharesDeposit { get; set; }
    public long SharesTotal { get; set; }
    public long RightsNonDeposit { get; set; }
    public long RightsDeposit { get; set; }
    public long VotingRights { get; set; }                      // Col 16 — mandatory

    // Classification tags (derived from VSDC section headers)
    public bool IsOrganization { get; set; }                    // "2. Tổ chức"
    public bool IsForeign { get; set; }                         // "II. MÔI GIỚI NƯỚC NGOÀI"

    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}
