namespace Mms.Application.Shareholders.Dtos;

public record ImportResultDto(
    int TotalRead,
    int Inserted,
    int Updated,
    int Skipped,
    int IndividualCount,
    int OrganizationCount,
    int DomesticCount,
    int ForeignCount,
    long TotalVotingShares,
    TimeSpan ElapsedTime);
