using Microsoft.EntityFrameworkCore;
using Mms.Application.Common.Interfaces;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IDbContextFactory<MmsDbContext> _factory;

    public AuditLogService(IDbContextFactory<MmsDbContext> factory) => _factory = factory;

    public async Task LogAsync(AuditCategory category, string entityType,
        Guid? entityId, string detail, Guid? userId, string actor,
        Guid? meetingId = null, CancellationToken ct = default)
    {
        // Separate context = separate transaction — survives business logic rollback
        await using var ctx = await _factory.CreateDbContextAsync(ct);
        ctx.AuditLogs.Add(new AuditLog
        {
            Category = category,
            EntityType = entityType,
            EntityId = entityId,
            Detail = (detail != null && (detail.StartsWith("{") || detail.StartsWith("["))) 
                ? detail 
                : System.Text.Json.JsonSerializer.Serialize(new { Message = detail }),
            UserId = userId,
            Actor = actor,
            MeetingId = meetingId,
            Ts = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync(ct);
    }
}
