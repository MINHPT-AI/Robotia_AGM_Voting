namespace Mms.Application.Shareholders.Dtos;

public class ShareholderImportDto
{
    public int RowIndex { get; set; }        // Dòng gốc Excel (debug)
    public int DisplayOrder { get; set; }
    public string VsdcRow { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Sid { get; set; }
    public string? InvestorCode { get; set; }
    public string IdNumber { get; set; } = "";
    public DateOnly? IdIssueDate { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Nationality { get; set; }
    public long SharesNonDeposit { get; set; }
    public long SharesDeposit { get; set; }
    public long SharesTotal { get; set; }
    public long RightsNonDeposit { get; set; }
    public long RightsDeposit { get; set; }
    public long VotingRights { get; set; }
    public bool IsOrganization { get; set; }
    public bool IsForeign { get; set; }
}
