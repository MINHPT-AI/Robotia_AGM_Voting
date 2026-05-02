namespace Mms.Application.Meetings.Dtos;

public record ResolutionDto(Guid? Id, int DisplayOrder, string Title, string? Content);
