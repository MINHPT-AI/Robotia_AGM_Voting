using Mms.Domain.Enums;

namespace Mms.Domain.Entities;

/// <summary>
/// Append-only audit log. Uses BIGSERIAL Id (not BaseEntity UUID).
/// Protected by DB trigger preventing UPDATE/DELETE.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    public DateTime Ts { get; set; } = DateTime.UtcNow;
    public Guid? UserId { get; set; }
    public string Actor { get; set; } = string.Empty;
    public AuditCategory Category { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public Guid? MeetingId { get; set; }
    public string? Detail { get; set; } // JSONB stored as string
    public string? PosTerminal { get; set; }
}
