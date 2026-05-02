using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Shareholders.Queries;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Shareholders;

public class SearchShareholdersHandler
    : IRequestHandler<SearchShareholdersQuery, IList<ShareholderSearchResultDto>>
{
    private readonly MmsDbContext _db;
    public SearchShareholdersHandler(MmsDbContext db) => _db = db;

    public async Task<IList<ShareholderSearchResultDto>> Handle(
        SearchShareholdersQuery query, CancellationToken ct)
    {
        var term = query.SearchTerm.Trim().ToLower();

        var shareholders = await _db.Shareholders
            .Where(s => s.MeetingId == query.MeetingId
                && (s.FullName.ToLower().Contains(term)
                    || s.IdNumber.ToLower().Contains(term)
                    || (s.InvestorCode != null && s.InvestorCode.ToLower().Contains(term))))
            .Take(15)
            .Select(s => new ShareholderSearchResultDto(s.Id, s.FullName, s.IdNumber, s.VotingRights))
            .ToListAsync(ct);

        // Mở rộng tìm kiếm sang danh sách ProxyRecipient bên ngoài (chỉ những người CÓ UỶ QUYỀN trong cuộc họp này)
        var recipients = await _db.ProxyRecipients
            .Where(r => r.FullName.ToLower().Contains(term) || r.IdNumber.ToLower().Contains(term))
            .Take(15)
            .ToListAsync(ct);

        if (recipients.Any())
        {
            var recipientIds = recipients.Select(r => r.Id).ToList();
            var validRecipientIds = await _db.Proxies
                .Where(p => p.MeetingId == query.MeetingId && p.GranteeRecipientId != null && recipientIds.Contains(p.GranteeRecipientId.Value))
                .Select(p => p.GranteeRecipientId!.Value)
                .Distinct()
                .ToListAsync(ct);

            // Chỉ lấy những recipient có proxy hợp lệ, và LỌC TRÙNG THEO CCCD
            var uniqueValidRecipients = recipients
                .Where(r => validRecipientIds.Contains(r.Id))
                .GroupBy(r => r.IdNumber)
                .Select(g => g.First())
                .ToList();

            // Lấy danh sách CCCD đã có trong kết quả VSDC để tránh trùng
            var existingIdNumbers = shareholders.Select(s => s.IdNumber).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var r in uniqueValidRecipients)
            {
                if (!existingIdNumbers.Contains(r.IdNumber))
                {
                    // Dùng Guid ngẫu nhiên vì UI Checkin chỉ dùng IdNumber để trigger PerformSearch()
                    shareholders.Add(new ShareholderSearchResultDto(Guid.NewGuid(), r.FullName, r.IdNumber, 0));
                    existingIdNumbers.Add(r.IdNumber);
                }
            }
        }

        return shareholders;
    }
}
