using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Shareholders.Dtos;
using Mms.Application.Shareholders.Queries;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Shareholders;

public class GetShareholdersByMeetingHandler
    : IRequestHandler<GetShareholdersByMeetingQuery, List<ShareholderListDto>>
{
    private readonly MmsDbContext _db;

    public GetShareholdersByMeetingHandler(MmsDbContext db) => _db = db;

    public async Task<List<ShareholderListDto>> Handle(
        GetShareholdersByMeetingQuery req, CancellationToken ct)
    {
        return await _db.Shareholders
            .Where(s => s.MeetingId == req.MeetingId)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new ShareholderListDto(
                s.Id, s.DisplayOrder, s.VsdcRow ?? "",
                s.FullName, s.Sid, s.InvestorCode, s.IdNumber,
                s.IdIssueDate, s.Address, s.Email, s.Phone,
                s.Nationality,
                s.SharesNonDeposit, s.SharesDeposit, s.SharesTotal,
                s.RightsNonDeposit, s.RightsDeposit, s.VotingRights,
                s.IsOrganization, s.IsForeign))
            .ToListAsync(ct);
    }
}
