using Mms.Domain.Enums;

namespace Mms.Application.Common.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(AuditCategory category, string entityType,
                  Guid? entityId, string detail,
                  Guid? userId, string actor,
                  Guid? meetingId = null,
                  CancellationToken ct = default);
}
