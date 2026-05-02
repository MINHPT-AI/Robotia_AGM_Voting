namespace Mms.Application.Meetings.Dtos;

public record CandidateDto(
    Guid? Id,
    int DisplayOrder,
    string FullName,
    string Position,        // "HĐQT" or "BKS"
    string? CurrentPosition,
    int? BirthYear,
    string? Notes
);
