namespace Mms.Application.Companies.Dtos;

public record CompanyDto(
    Guid? Id,
    string Name,
    string? ShortName,
    string? EnglishName,
    string TaxCode,
    string? StockCode,
    string? StockExchange,
    string? Address,
    string? Phone,
    string? Email,
    string? Fax,
    string? Website,
    string LegalRepName,
    string LegalRepTitle,
    long CharterCapital,
    long TotalSharesIssued,
    long TotalVotingShares,
    string? LogoPath,
    string? SealImagePath,
    string? SignatureImagePath
);
