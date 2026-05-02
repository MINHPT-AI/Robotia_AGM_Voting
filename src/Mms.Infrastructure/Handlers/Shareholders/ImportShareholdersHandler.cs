using System.Diagnostics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Mms.Application.Common.Interfaces;
using Mms.Application.Shareholders.Commands;
using Mms.Application.Shareholders.Dtos;
using Mms.Domain.Enums;
using Mms.Infrastructure.Persistence;
using Npgsql;
using NpgsqlTypes;

namespace Mms.Infrastructure.Handlers.Shareholders;

public class ImportShareholdersHandler
    : IRequestHandler<ImportShareholdersCommand, ImportResultDto>
{
    private readonly MmsDbContext _db;
    private readonly IAuditLogService _audit;
    private readonly ILogger<ImportShareholdersHandler> _logger;

    public ImportShareholdersHandler(
        MmsDbContext db, IAuditLogService audit,
        ILogger<ImportShareholdersHandler> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
    }

    public async Task<ImportResultDto> Handle(
        ImportShareholdersCommand req, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var rows = req.ValidRows;
        if (rows.Count == 0)
            return new(0, 0, 0, 0, 0, 0, 0, 0, 0, sw.Elapsed);

        // ⚡ DELETE old + INSERT new in one transaction
        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Step 1: Delete all existing shareholders for this meeting
            var deleted = await _db.Database.ExecuteSqlRawAsync(
                @"DELETE FROM shareholders WHERE ""MeetingId"" = {0}",
                new object[] { req.MeetingId }, ct);
            _logger.LogInformation("Deleted {Count} existing shareholders for meeting {MeetingId}",
                deleted, req.MeetingId);

            // Step 2: Bulk INSERT in batches
            const int batchSize = 500;
            for (int i = 0; i < rows.Count; i += batchSize)
            {
                var batch = rows.Skip(i).Take(batchSize).ToList();
                await InsertBatchAsync(req.MeetingId, batch, ct);
            }

            await tx.CommitAsync(ct);

            try
            {
                await _audit.LogAsync(
                    AuditCategory.Shareholder, nameof(Domain.Entities.Shareholder),
                    req.MeetingId,
                    $"VSDC Import: {rows.Count} rows inserted (replaced {deleted} old rows)",
                    null, "system", req.MeetingId, ct);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed, continuing"); }
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        // Stats breakdown
        var indCount = rows.Count(r => !r.IsOrganization);
        var orgCount = rows.Count(r => r.IsOrganization);
        var domCount = rows.Count(r => !r.IsForeign);
        var forCount = rows.Count(r => r.IsForeign);
        var totalVR = rows.Sum(r => r.VotingRights);

        sw.Stop();
        _logger.LogInformation(
            "VSDC Import completed: {Count} rows in {Elapsed}ms",
            rows.Count, sw.ElapsedMilliseconds);

        return new(rows.Count, rows.Count, 0, 0,
            indCount, orgCount, domCount, forCount, totalVR, sw.Elapsed);
    }

    private async Task InsertBatchAsync(
        Guid meetingId, List<ShareholderImportDto> batch, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);

        var npgsqlConn = (NpgsqlConnection)conn;

        // Simple INSERT — no ON CONFLICT needed since we deleted all rows first
        var sql = @"
INSERT INTO shareholders
(""Id"", ""MeetingId"", ""VsdcRow"", ""DisplayOrder"", ""FullName"", ""Sid"",
 ""InvestorCode"", ""IdNumber"", ""IdIssueDate"", ""Address"", ""Email"", ""Phone"",
 ""Nationality"", ""SharesNonDeposit"", ""SharesDeposit"", ""SharesTotal"",
 ""RightsNonDeposit"", ""RightsDeposit"", ""VotingRights"",
 ""IsOrganization"", ""IsForeign"", ""CreatedAt"", ""ImportedAt"")
SELECT
    gen_random_uuid(), @p_meeting,
    unnest(@p_vsdc_rows), unnest(@p_display_orders), unnest(@p_full_names), unnest(@p_sids),
    unnest(@p_inv_codes), unnest(@p_id_numbers), unnest(@p_id_dates), unnest(@p_addresses),
    unnest(@p_emails), unnest(@p_phones), unnest(@p_nationalities),
    unnest(@p_shares_nd), unnest(@p_shares_d), unnest(@p_shares_total),
    unnest(@p_rights_nd), unnest(@p_rights_d), unnest(@p_voting_rights),
    unnest(@p_is_orgs), unnest(@p_is_foreigns), NOW(), NOW();
";

        await using var cmd = new NpgsqlCommand(sql, npgsqlConn);
        if (npgsqlConn.State != System.Data.ConnectionState.Open)
            await npgsqlConn.OpenAsync(ct);

        cmd.Transaction = (NpgsqlTransaction?)_db.Database.CurrentTransaction?.GetDbTransaction();

        cmd.Parameters.AddWithValue("p_meeting", meetingId);
        cmd.Parameters.AddWithValue("p_vsdc_rows", batch.Select(r => r.VsdcRow).ToArray());
        cmd.Parameters.AddWithValue("p_display_orders", batch.Select(r => r.DisplayOrder).ToArray());
        cmd.Parameters.AddWithValue("p_full_names", batch.Select(r => r.FullName).ToArray());

        // Nullable text[] — explicit NpgsqlDbType
        cmd.Parameters.Add(new NpgsqlParameter("p_sids", NpgsqlDbType.Text | NpgsqlDbType.Array)
            { Value = batch.Select(r => r.Sid).ToArray() });
        cmd.Parameters.Add(new NpgsqlParameter("p_inv_codes", NpgsqlDbType.Text | NpgsqlDbType.Array)
            { Value = batch.Select(r => r.InvestorCode).ToArray() });

        cmd.Parameters.AddWithValue("p_id_numbers", batch.Select(r => r.IdNumber).ToArray());

        // DateOnly? → NpgsqlDbType.Date array
        var idDates = batch.Select(r => r.IdIssueDate.HasValue
            ? (object)r.IdIssueDate.Value : DBNull.Value).ToArray();
        cmd.Parameters.Add(new NpgsqlParameter("p_id_dates", NpgsqlDbType.Date | NpgsqlDbType.Array)
            { Value = idDates });

        // Nullable text[] fields
        cmd.Parameters.Add(new NpgsqlParameter("p_addresses", NpgsqlDbType.Text | NpgsqlDbType.Array)
            { Value = batch.Select(r => r.Address).ToArray() });
        cmd.Parameters.Add(new NpgsqlParameter("p_emails", NpgsqlDbType.Text | NpgsqlDbType.Array)
            { Value = batch.Select(r => r.Email).ToArray() });
        cmd.Parameters.Add(new NpgsqlParameter("p_phones", NpgsqlDbType.Text | NpgsqlDbType.Array)
            { Value = batch.Select(r => r.Phone).ToArray() });
        cmd.Parameters.Add(new NpgsqlParameter("p_nationalities", NpgsqlDbType.Text | NpgsqlDbType.Array)
            { Value = batch.Select(r => r.Nationality).ToArray() });

        // Non-nullable numeric arrays
        cmd.Parameters.AddWithValue("p_shares_nd", batch.Select(r => r.SharesNonDeposit).ToArray());
        cmd.Parameters.AddWithValue("p_shares_d", batch.Select(r => r.SharesDeposit).ToArray());
        cmd.Parameters.AddWithValue("p_shares_total", batch.Select(r => r.SharesTotal).ToArray());
        cmd.Parameters.AddWithValue("p_rights_nd", batch.Select(r => r.RightsNonDeposit).ToArray());
        cmd.Parameters.AddWithValue("p_rights_d", batch.Select(r => r.RightsDeposit).ToArray());
        cmd.Parameters.AddWithValue("p_voting_rights", batch.Select(r => r.VotingRights).ToArray());
        cmd.Parameters.AddWithValue("p_is_orgs", batch.Select(r => r.IsOrganization).ToArray());
        cmd.Parameters.AddWithValue("p_is_foreigns", batch.Select(r => r.IsForeign).ToArray());

        await cmd.ExecuteNonQueryAsync(ct);
    }
}
