using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Proxies.Commands;
using Mms.Application.Proxies.Dtos;
using Mms.Application.Proxies.Queries;
using Mms.Domain.Enums;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Proxies;

public class GetShareholderProxyStatusHandler
    : IRequestHandler<GetShareholderProxyStatusQuery, ShareholderProxyStatusDto?>
{
    private readonly MmsDbContext _db;

    public GetShareholderProxyStatusHandler(MmsDbContext db) => _db = db;

    public async Task<ShareholderProxyStatusDto?> Handle(
        GetShareholderProxyStatusQuery query, CancellationToken ct)
    {
        var sh = await _db.Shareholders
            .FirstOrDefaultAsync(s => s.Id == query.ShareholderId && s.MeetingId == query.MeetingId, ct);
        if (sh is null) return null;

        var activeStatuses = new[] { ProxyStatus.Pending, ProxyStatus.Confirmed };

        // Ủy quyền đi (outgoing)
        var outgoing = await _db.Proxies
            .Where(p => p.MeetingId == query.MeetingId
                     && p.GrantorId == query.ShareholderId
                     && activeStatuses.Contains(p.Status))
            .Select(p => new ProxyListItemDto(
                p.Id, p.GrantorId, p.Grantor.FullName, p.Grantor.IdNumber, p.Grantor.VotingRights,
                p.GranteeName, p.GranteeIdNumber, p.Shares, p.Scope, p.ProxyType,
                p.Status, p.ProxyDate, p.CreatedAt))
            .ToListAsync(ct);

        // Ủy quyền nhận (incoming)
        var incoming = await _db.Proxies
            .Where(p => p.MeetingId == query.MeetingId
                     && p.GranteeShareholderId == query.ShareholderId
                     && activeStatuses.Contains(p.Status))
            .Select(p => new ProxyListItemDto(
                p.Id, p.GrantorId, p.Grantor.FullName, p.Grantor.IdNumber, p.Grantor.VotingRights,
                p.GranteeName, p.GranteeIdNumber, p.Shares, p.Scope, p.ProxyType,
                p.Status, p.ProxyDate, p.CreatedAt))
            .ToListAsync(ct);

        var sharesAlreadyProxied = outgoing.Sum(p => p.Shares);

        return new ShareholderProxyStatusDto(
            sh.Id, sh.FullName, sh.IdNumber, sh.VotingRights,
            sharesAlreadyProxied,
            sh.VotingRights - sharesAlreadyProxied,
            incoming.Count > 0,
            incoming.Sum(p => p.Shares),
            outgoing, incoming);
    }
}

public class GetAllProxiesHandler : IRequestHandler<GetAllProxiesQuery, IList<ProxyListItemDto>>
{
    private readonly MmsDbContext _db;
    public GetAllProxiesHandler(MmsDbContext db) => _db = db;

    public async Task<IList<ProxyListItemDto>> Handle(GetAllProxiesQuery query, CancellationToken ct)
    {
        return await _db.Proxies
            .Where(p => p.MeetingId == query.MeetingId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProxyListItemDto(
                p.Id, p.GrantorId, p.Grantor.FullName, p.Grantor.IdNumber, p.Grantor.VotingRights,
                p.GranteeName, p.GranteeIdNumber, p.Shares, p.Scope, p.ProxyType,
                p.Status, p.ProxyDate, p.CreatedAt))
            .ToListAsync(ct);
    }
}

public class GetProxyTopbarHandler : IRequestHandler<GetProxyTopbarQuery, ProxyTopbarDto>
{
    private readonly MmsDbContext _db;
    public GetProxyTopbarHandler(MmsDbContext db) => _db = db;

    public async Task<ProxyTopbarDto> Handle(GetProxyTopbarQuery query, CancellationToken ct)
    {
        var activeStatuses = new[] { ProxyStatus.Pending, ProxyStatus.Confirmed };

        var proxies = await _db.Proxies
            .Where(p => p.MeetingId == query.MeetingId && activeStatuses.Contains(p.Status))
            .Select(p => new { p.GrantorId, p.Shares, p.Status })
            .ToListAsync(ct);

        return new ProxyTopbarDto(
            proxies.Select(p => p.GrantorId).Distinct().Count(),
            proxies.Sum(p => p.Shares),
            proxies.Count(p => p.Status == ProxyStatus.Pending));
    }
}

public class SearchProxyRecipientsHandler : IRequestHandler<SearchProxyRecipientsQuery, IList<ProxyRecipientDto>>
{
    private readonly MmsDbContext _db;
    public SearchProxyRecipientsHandler(MmsDbContext db) => _db = db;

    public async Task<IList<ProxyRecipientDto>> Handle(SearchProxyRecipientsQuery query, CancellationToken ct)
    {
        return await _db.ProxyRecipients
            .Where(r => r.IdNumber.StartsWith(query.IdNumberPrefix))
            .Take(10)
            .Select(r => new ProxyRecipientDto(r.Id, r.FullName, r.IdNumber, r.Organization, r.Position, r.PhoneNumber))
            .ToListAsync(ct);
    }
}

public class GetAvailableSharesHandler : IRequestHandler<GetAvailableSharesQuery, long>
{
    private readonly MmsDbContext _db;
    public GetAvailableSharesHandler(MmsDbContext db) => _db = db;

    public async Task<long> Handle(GetAvailableSharesQuery query, CancellationToken ct)
    {
        var sh = await _db.Shareholders
            .FirstOrDefaultAsync(s => s.Id == query.ShareholderId && s.MeetingId == query.MeetingId, ct)
            ?? throw new InvalidOperationException("Cổ đông không tồn tại.");

        var proxied = await _db.Proxies
            .Where(p => p.MeetingId == query.MeetingId
                     && p.GrantorId == query.ShareholderId
                     && (p.Status == ProxyStatus.Pending || p.Status == ProxyStatus.Confirmed))
            .SumAsync(p => p.Shares, ct);

        return sh.VotingRights - proxied;
    }
}

public class GetGranteeGroupsHandler : IRequestHandler<GetGranteeGroupsQuery, IList<GranteeGroupDto>>
{
    private readonly MmsDbContext _db;
    public GetGranteeGroupsHandler(MmsDbContext db) => _db = db;

    public async Task<IList<GranteeGroupDto>> Handle(GetGranteeGroupsQuery query, CancellationToken ct)
    {
        var activeStatuses = new[] { ProxyStatus.Pending, ProxyStatus.Confirmed };

        var proxiesQuery = _db.Proxies
            .Where(p => p.MeetingId == query.MeetingId && activeStatuses.Contains(p.Status));

        // Filter by search text if provided
        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var search = query.SearchText.Trim().ToLower();
            proxiesQuery = proxiesQuery.Where(p =>
                p.GranteeName.ToLower().Contains(search) ||
                (p.GranteeIdNumber != null && p.GranteeIdNumber.ToLower().Contains(search)));
        }

        var proxies = await proxiesQuery
            .OrderBy(p => p.GranteeName)
            .Select(p => new ProxyListItemDto(
                p.Id, p.GrantorId, p.Grantor.FullName, p.Grantor.IdNumber, p.Grantor.VotingRights,
                p.GranteeName, p.GranteeIdNumber, p.Shares, p.Scope, p.ProxyType,
                p.Status, p.ProxyDate, p.CreatedAt))
            .ToListAsync(ct);

        // Group by ID Number (if present) or by normalized Name (if no ID)
        return proxies
            .GroupBy(p => string.IsNullOrWhiteSpace(p.GranteeIdNumber) 
                ? "NAME_" + p.GranteeName.Trim().ToLower() 
                : "ID_" + p.GranteeIdNumber.Trim().ToLower())
            .Select(g => new GranteeGroupDto(
                g.First().GranteeName, // Lấy tên gốc của dòng đầu tiên
                g.First().GranteeIdNumber, // Lấy số ĐKSH gốc
                g.Select(p => p.GrantorId).Distinct().Count(),
                g.Sum(p => p.Shares),
                g.Count(p => p.Status == ProxyStatus.Confirmed),
                g.Count(p => p.Status == ProxyStatus.Pending),
                g.ToList()))
            .OrderByDescending(g => g.TotalShares)
            .ToList();
    }
}

public class GetGrantorGroupsHandler : IRequestHandler<GetGrantorGroupsQuery, IList<GrantorGroupDto>>
{
    private readonly MmsDbContext _db;
    public GetGrantorGroupsHandler(MmsDbContext db) => _db = db;

    public async Task<IList<GrantorGroupDto>> Handle(GetGrantorGroupsQuery query, CancellationToken ct)
    {
        var activeStatuses = new[] { ProxyStatus.Pending, ProxyStatus.Confirmed };

        // Start with shareholders who have at least one active proxy
        var grantorIds = await _db.Proxies
            .Where(p => p.MeetingId == query.MeetingId && activeStatuses.Contains(p.Status))
            .Select(p => p.GrantorId)
            .Distinct()
            .ToListAsync(ct);

        // Load shareholders for name/ID filter
        var shareholdersQuery = _db.Shareholders
            .Where(s => s.MeetingId == query.MeetingId && grantorIds.Contains(s.Id));

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var search = query.SearchText.Trim().ToLower();
            shareholdersQuery = shareholdersQuery.Where(s =>
                s.FullName.ToLower().Contains(search) ||
                s.IdNumber.ToLower().Contains(search));
        }

        var shareholders = await shareholdersQuery.ToListAsync(ct);
        var filteredIds = shareholders.Select(s => s.Id).ToHashSet();

        // Load proxies for filtered grantors
        var proxies = await _db.Proxies
            .Where(p => p.MeetingId == query.MeetingId
                     && activeStatuses.Contains(p.Status)
                     && filteredIds.Contains(p.GrantorId))
            .OrderBy(p => p.GranteeName)
            .Select(p => new ProxyListItemDto(
                p.Id, p.GrantorId, p.Grantor.FullName, p.Grantor.IdNumber, p.Grantor.VotingRights,
                p.GranteeName, p.GranteeIdNumber, p.Shares, p.Scope, p.ProxyType,
                p.Status, p.ProxyDate, p.CreatedAt))
            .ToListAsync(ct);

        // Group by grantor
        return proxies
            .GroupBy(p => p.GrantorId)
            .Select(g =>
            {
                var sh = shareholders.First(s => s.Id == g.Key);
                var sharesProxied = g.Sum(p => p.Shares);
                return new GrantorGroupDto(
                    g.Key,
                    sh.FullName,
                    sh.IdNumber,
                    sh.VotingRights,
                    sharesProxied,
                    sh.VotingRights - sharesProxied,
                    g.Select(p => p.GranteeName).Distinct().Count(),
                    g.Count(p => p.Status == ProxyStatus.Confirmed),
                    g.Count(p => p.Status == ProxyStatus.Pending),
                    g.ToList());
            })
            .OrderByDescending(g => g.SharesProxied)
            .ToList();
    }
}

public class ValidateProxiesImportHandler : IRequestHandler<ValidateProxiesImportQuery, ProxyImportResultDto>
{
    private readonly MmsDbContext _db;
    public ValidateProxiesImportHandler(MmsDbContext db) => _db = db;

    public async Task<ProxyImportResultDto> Handle(ValidateProxiesImportQuery query, CancellationToken ct)
    {
        var shareholders = await _db.Shareholders
            .Where(s => s.MeetingId == query.MeetingId)
            .ToListAsync(ct);

        var shByIdNumber = shareholders
            .GroupBy(s => s.IdNumber.Trim().ToLower())
            .ToDictionary(g => g.Key, g => g.First());

        var activeStatuses = new[] { ProxyStatus.Pending, ProxyStatus.Confirmed };
        var existingProxies = await _db.Proxies
            .Where(p => p.MeetingId == query.MeetingId && activeStatuses.Contains(p.Status))
            .Select(p => new { p.GrantorId, p.Shares })
            .ToListAsync(ct);

        var availableShares = shareholders.ToDictionary(
            s => s.Id,
            s => s.VotingRights - existingProxies.Where(p => p.GrantorId == s.Id).Sum(p => p.Shares));

        var validationRows = new List<ProxyImportValidationRow>();
        int successCount = 0;

        foreach (var row in query.Rows)
        {
            var grantorKey = row.GrantorIdNumber.Trim().ToLower();

            if (!shByIdNumber.TryGetValue(grantorKey, out var grantor))
            {
                validationRows.Add(new ProxyImportValidationRow(
                    row.RowNumber, row.Stt, row.GrantorName, row.GrantorIdNumber,
                    row.GranteeName, row.GranteeIdNumber, row.Shares, row.GranteePhone,
                    false, $"Không tìm thấy CĐ '{row.GrantorName}' (ĐKSH: {row.GrantorIdNumber}) trong DS.", 0));
                continue;
            }

            var available = availableShares.GetValueOrDefault(grantor.Id, 0);

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

            // Deduct for subsequent rows of same grantor
            availableShares[grantor.Id] = available - row.Shares;

            validationRows.Add(new ProxyImportValidationRow(
                row.RowNumber, row.Stt, row.GrantorName, row.GrantorIdNumber,
                row.GranteeName, row.GranteeIdNumber, row.Shares, row.GranteePhone,
                true, null, available));
            successCount++;
        }

        return new ProxyImportResultDto(query.Rows.Count, successCount, query.Rows.Count - successCount, validationRows);
    }
}
