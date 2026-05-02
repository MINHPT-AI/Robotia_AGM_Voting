using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Common.Interfaces;
using Mms.Application.Proxies.Commands;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Proxies;

public class ImportProxiesHandler : IRequestHandler<ImportProxiesCommand, ProxyImportResultDto>
{
    private readonly MmsDbContext _db;
    private readonly IAuditLogService _audit;

    public ImportProxiesHandler(MmsDbContext db, IAuditLogService audit)
        => (_db, _audit) = (db, audit);

    public async Task<ProxyImportResultDto> Handle(ImportProxiesCommand cmd, CancellationToken ct)
    {
        // ── 1. Load all shareholders for this meeting ──
        var shareholders = await _db.Shareholders
            .Where(s => s.MeetingId == cmd.MeetingId)
            .ToListAsync(ct);

        var shByIdNumber = shareholders
            .GroupBy(s => s.IdNumber.Trim().ToLower())
            .ToDictionary(g => g.Key, g => g.First());

        // ── 2. Load existing active proxies to calculate available shares ──
        var activeStatuses = new[] { ProxyStatus.Pending, ProxyStatus.Confirmed };
        var existingProxies = await _db.Proxies
            .Where(p => p.MeetingId == cmd.MeetingId && activeStatuses.Contains(p.Status))
            .Select(p => new { p.GrantorId, p.Shares })
            .ToListAsync(ct);

        // Running available shares tracker (decrements as we process valid rows)
        var availableShares = shareholders.ToDictionary(
            s => s.Id,
            s => s.VotingRights - existingProxies.Where(p => p.GrantorId == s.Id).Sum(p => p.Shares));

        // ── 3. Validate Meeting ──
        var meeting = await _db.Meetings.FindAsync(new object[] { cmd.MeetingId }, ct)
            ?? throw new InvalidOperationException("Cuộc họp không tồn tại.");

        if (meeting.Status == MeetingStatus.Tallying || meeting.Status == MeetingStatus.Completed)
            throw new InvalidOperationException("Không thể import ủy quyền sau khi đã bắt đầu kiểm phiếu.");

        // ── 4. Validate each row ──
        var validationRows = new List<ProxyImportValidationRow>();
        var proxiesToCreate = new List<(ProxyImportRowDto Row, Guid GrantorId, Guid? GranteeShId, ProxyScope Scope)>();

        foreach (var row in cmd.Rows)
        {
            var grantorKey = row.GrantorIdNumber.Trim().ToLower();

            // Check grantor exists
            if (!shByIdNumber.TryGetValue(grantorKey, out var grantor))
            {
                validationRows.Add(new ProxyImportValidationRow(
                    row.RowNumber, row.Stt, row.GrantorName, row.GrantorIdNumber,
                    row.GranteeName, row.GranteeIdNumber, row.Shares, row.GranteePhone,
                    false, $"Không tìm thấy CĐ ủy quyền '{row.GrantorName}' (ĐKSH: {row.GrantorIdNumber}) trong DS cuộc họp.", 0));
                continue;
            }

            var available = availableShares.GetValueOrDefault(grantor.Id, 0);

            // Check shares
            if (row.Shares <= 0)
            {
                validationRows.Add(new ProxyImportValidationRow(
                    row.RowNumber, row.Stt, row.GrantorName, row.GrantorIdNumber,
                    row.GranteeName, row.GranteeIdNumber, row.Shares, row.GranteePhone,
                    false, "Số cổ phần phải lớn hơn 0.", available));
                continue;
            }

            if (row.Shares > available)
            {
                validationRows.Add(new ProxyImportValidationRow(
                    row.RowNumber, row.Stt, row.GrantorName, row.GrantorIdNumber,
                    row.GranteeName, row.GranteeIdNumber, row.Shares, row.GranteePhone,
                    false, $"Vượt quá CP khả dụng ({available:N0}). Yêu cầu: {row.Shares:N0}.", available));
                continue;
            }

            // Check grantee - try find in shareholders first
            var granteeKey = row.GranteeIdNumber.Trim().ToLower();
            Guid? granteeShId = null;
            if (shByIdNumber.TryGetValue(granteeKey, out var grantee))
            {
                granteeShId = grantee.Id;
            }

            // Determine scope
            var scope = row.Shares == available ? ProxyScope.Full : ProxyScope.Partial;

            // Deduct from available (for subsequent rows of the same grantor)
            availableShares[grantor.Id] = available - row.Shares;

            validationRows.Add(new ProxyImportValidationRow(
                row.RowNumber, row.Stt, row.GrantorName, row.GrantorIdNumber,
                row.GranteeName, row.GranteeIdNumber, row.Shares, row.GranteePhone,
                true, null, available));

            proxiesToCreate.Add((row, grantor.Id, granteeShId, scope));
        }

        // ── 5. Create valid proxies ──
        int successCount = 0;
        foreach (var (row, grantorId, granteeShId, scope) in proxiesToCreate)
        {
            Guid? granteeRecipientId = null;
            string granteeName = row.GranteeName;
            string granteeIdNumber = row.GranteeIdNumber;

            if (granteeShId == null)
            {
                // External grantee → lookup or create ProxyRecipient
                var existing = await _db.ProxyRecipients
                    .FirstOrDefaultAsync(r => r.IdNumber == row.GranteeIdNumber, ct);

                if (existing != null)
                {
                    granteeRecipientId = existing.Id;
                    if (!string.IsNullOrWhiteSpace(row.GranteePhone) && existing.PhoneNumber != row.GranteePhone)
                    {
                        existing.PhoneNumber = row.GranteePhone;
                        existing.PhoneUpdatedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    var recipient = new ProxyRecipient
                    {
                        FullName = row.GranteeName,
                        IdNumber = row.GranteeIdNumber,
                        PhoneNumber = row.GranteePhone,
                        PhoneUpdatedAt = !string.IsNullOrWhiteSpace(row.GranteePhone) ? DateTime.UtcNow : null,
                    };
                    _db.ProxyRecipients.Add(recipient);
                    granteeRecipientId = recipient.Id;
                }
            }
            else
            {
                // Internal grantee
                var granteeSh = shByIdNumber[row.GranteeIdNumber.Trim().ToLower()];
                granteeName = granteeSh.FullName;
                granteeIdNumber = granteeSh.IdNumber;
            }

            var proxy = new Proxy
            {
                MeetingId = cmd.MeetingId,
                GrantorId = grantorId,
                GranteeName = granteeName,
                GranteeIdNumber = granteeIdNumber,
                Shares = row.Shares,
                Scope = scope,
                ProxyType = ProxyType.PreMeeting,
                ProxyDate = DateOnly.FromDateTime(DateTime.Today),
                Detail = "Import từ Excel",
                Status = ProxyStatus.Confirmed,
                GranteeShareholderId = granteeShId,
                GranteeRecipientId = granteeRecipientId,
            };

            _db.Proxies.Add(proxy);
            successCount++;
        }

        await _db.SaveChangesAsync(ct);

        // ── 6. Audit ──
        try
        {
            await _audit.LogAsync(AuditCategory.Proxy, "ProxyImport", cmd.MeetingId,
                $"Import {successCount}/{cmd.Rows.Count} ủy quyền từ Excel",
                cmd.OperatorUserId, cmd.Actor, cmd.MeetingId, ct);
        }
        catch { /* audit failure should not block */ }

        return new ProxyImportResultDto(
            cmd.Rows.Count,
            successCount,
            cmd.Rows.Count - successCount,
            validationRows);
    }
}
