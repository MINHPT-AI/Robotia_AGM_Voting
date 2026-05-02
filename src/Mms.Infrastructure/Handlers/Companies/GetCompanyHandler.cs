using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Companies.Dtos;
using Mms.Application.Companies.Queries;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Companies;

public class GetCompanyHandler : IRequestHandler<GetCompanyQuery, CompanyDto?>
{
    private readonly MmsDbContext _db;
    public GetCompanyHandler(MmsDbContext db) => _db = db;

    public async Task<CompanyDto?> Handle(GetCompanyQuery request, CancellationToken ct)
    {
        var c = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(ct);
        if (c is null) return null;
        return new CompanyDto(c.Id, c.Name, c.ShortName, c.EnglishName, c.TaxCode,
            c.StockCode, c.StockExchange, c.Address, c.Phone, c.Email, c.Fax, c.Website,
            c.LegalRepName, c.LegalRepTitle, c.CharterCapital, c.TotalSharesIssued,
            c.TotalVotingShares, c.LogoPath, c.SealImagePath, c.SignatureImagePath);
    }
}
