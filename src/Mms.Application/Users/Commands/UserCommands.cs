using MediatR;

namespace Mms.Application.Users.Commands;

public record CreateUserCommand(
    string UserName, string FullName, string? Email,
    string Password, string Role) : IRequest<Guid>;

public record UpdateUserCommand(
    Guid UserId, string FullName, string? Email, string NewRole) : IRequest;

public record ToggleUserActiveCommand(Guid UserId, bool SetActive) : IRequest;

public record AdminResetPasswordCommand(Guid UserId, string NewPassword) : IRequest;

public record UpdateProfileCommand(Guid UserId, string FullName, string? Email) : IRequest;

public record ChangeOwnPasswordCommand(
    Guid UserId, string CurrentPassword, string NewPassword) : IRequest;
