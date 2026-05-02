using Mms.Domain.Common;
using Mms.Domain.Enums;

namespace Mms.Domain.Entities;

public class Meeting : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public MeetingType MeetingType { get; set; }
    public MeetingStatus Status { get; set; } = MeetingStatus.New;
    public DateTime MeetingDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateOnly RecordDate { get; set; }
    public long TotalVotingShares { get; set; }
    public string? Chairman { get; set; }
    public string? Secretary { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }

    // ── Mở rộng theo BRD v2.3 ──

    /// <summary>Bật/tắt tính năng theo dõi quà tặng (BRD v2.3 Mục 5.5).</summary>
    public bool GiftEnabled { get; set; }

    /// <summary>Chế độ in phiếu mặc định: Gộp/Tách/Hybrid (BRD v2.3 Mục 5.3.1).</summary>
    public PrintMode DefaultPrintMode { get; set; } = PrintMode.Consolidated;

    /// <summary>Ngưỡng điều kiện tiến hành họp, mặc định 50% (BRD v2.3 Mục 6.1).</summary>
    public decimal QuorumThreshold { get; set; } = 0.50m;

    public ICollection<MeetingResolution> Resolutions { get; set; } = new List<MeetingResolution>();
    public ICollection<MeetingCandidate> Candidates { get; set; } = new List<MeetingCandidate>();
    public ICollection<Shareholder> Shareholders { get; set; } = new List<Shareholder>();
    public ICollection<InvitationLetter> InvitationLetters { get; set; } = new List<InvitationLetter>();
    public ICollection<MeetingTemplateConfig> TemplateConfigs { get; set; } = new List<MeetingTemplateConfig>();
}
