namespace Mms.Domain.Entities;

public class InvitationLetter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MeetingId { get; set; }
    public string ShareholderIdNumber { get; set; } = "";
    public string ShareholderName { get; set; } = "";
    public string? ShareholderAddress { get; set; }
    public string? ShareholderPhone { get; set; }
    public long VotingRights { get; set; }
    public long SharesTotal { get; set; }
    public InvitationStatus Status { get; set; } = InvitationStatus.NotSent;
    public string? TrackingCode { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? StatusUpdatedAt { get; set; }
    public string? FailureReason { get; set; }
    public CodeMarkType CodeMarkType { get; set; } = CodeMarkType.Barcode;
    public Guid? TemplateId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public Meeting Meeting { get; set; } = null!;
}

public enum InvitationStatus { NotSent = 0, Dispatched = 1, Delivered = 2, Failed = 3, Returned = 4 }
public enum CodeMarkType { None = 0, Barcode = 1, QRCode = 2 }
