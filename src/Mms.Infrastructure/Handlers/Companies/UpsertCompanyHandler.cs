using MediatR;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Common.Interfaces;
using Mms.Application.Companies.Commands;
using Mms.Application.Companies.Dtos;
using Mms.Domain.Entities;
using Mms.Domain.Enums;
using Mms.Infrastructure.Persistence;

namespace Mms.Infrastructure.Handlers.Companies;

public class UpsertCompanyHandler : IRequestHandler<UpsertCompanyCommand, CompanyDto>
{
    private readonly MmsDbContext _db;
    private readonly IAuditLogService _audit;

    public UpsertCompanyHandler(MmsDbContext db, IAuditLogService audit)
        => (_db, _audit) = (db, audit);

    public async Task<CompanyDto> Handle(UpsertCompanyCommand cmd, CancellationToken ct)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(ct);
        bool isNew = company is null;

        if (isNew)
        {
            company = new Company { Id = Guid.NewGuid() };
            _db.Companies.Add(company);
        }

        company!.Name = cmd.Name;
        company.ShortName = cmd.ShortName;
        company.EnglishName = cmd.EnglishName;
        company.TaxCode = cmd.TaxCode;
        company.StockCode = cmd.StockCode;
        company.StockExchange = cmd.StockExchange;
        company.Address = cmd.Address;
        company.Phone = cmd.Phone;
        company.Email = cmd.Email;
        company.Fax = cmd.Fax;
        company.Website = cmd.Website;
        company.LegalRepName = cmd.LegalRepName;
        company.LegalRepTitle = cmd.LegalRepTitle;
        company.CharterCapital = cmd.CharterCapital;
        company.TotalSharesIssued = cmd.TotalSharesIssued;
        company.TotalVotingShares = cmd.TotalVotingShares;
        company.LogoPath = cmd.LogoPath;
        company.SealImagePath = cmd.SealImagePath;
        company.SignatureImagePath = cmd.SignatureImagePath;
        company.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        try
        {
            await _audit.LogAsync(AuditCategory.Company, nameof(Company), company.Id,
                isNew ? "Company created" : "Company updated", null, "system", ct: ct);
        }
        catch { /* audit failure should not block save */ }

        return new CompanyDto(company.Id, company.Name, company.ShortName, company.EnglishName,
            company.TaxCode, company.StockCode, company.StockExchange, company.Address,
            company.Phone, company.Email, company.Fax, company.Website,
            company.LegalRepName, company.LegalRepTitle, company.CharterCapital,
            company.TotalSharesIssued, company.TotalVotingShares,
            company.LogoPath, company.SealImagePath, company.SignatureImagePath);
    }
}
