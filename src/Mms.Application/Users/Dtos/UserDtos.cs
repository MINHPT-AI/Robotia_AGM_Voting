namespace Mms.Application.Users.Dtos;

public record UserListItemDto(
    Guid Id, string UserName, string FullName, string? Email,
    string Role, bool IsActive, DateTime? LastLoginAt);

public record AuditLogDto(
    long Id, string EntityName, string? EntityId, string Action,
    string? Detail, string? PerformedBy, DateTime CreatedAt);
