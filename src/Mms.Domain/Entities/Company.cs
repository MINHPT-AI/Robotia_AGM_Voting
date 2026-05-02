using Mms.Domain.Common;

namespace Mms.Domain.Entities;

public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string TaxCode { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Fax { get; set; }
    public string? Website { get; set; }
    public string? EnglishName { get; set; }
    public string? StockExchange { get; set; }    // HOSE / HNX / UPCOM
    public string? LogoPath { get; set; }
    public string? SealImagePath { get; set; }
    public string? SignatureImagePath { get; set; }
    public string? StockCode { get; set; }
    public string LegalRepName { get; set; } = string.Empty;
    public string LegalRepTitle { get; set; } = string.Empty;
    public long CharterCapital { get; set; }
    public long TotalSharesIssued { get; set; }
    public long TotalVotingShares { get; set; }

    public ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();
}
