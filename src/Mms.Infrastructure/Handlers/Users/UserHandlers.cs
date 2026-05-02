using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mms.Application.Common.Models;
using Mms.Application.Users.Commands;
using Mms.Application.Users.Dtos;
using Mms.Application.Users.Queries;
using Mms.Infrastructure.Identity;
using Mms.Infrastructure.Persistence;
using AppValidationException = Mms.Application.Common.Exceptions.ValidationException;
using FluentValidation.Results;

namespace Mms.Infrastructure.Handlers.Users;

// ─── Command Handlers ───

public class CreateUserHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateUserHandler(UserManager<ApplicationUser> userManager)
        => _userManager = userManager;

    public async Task<Guid> Handle(CreateUserCommand cmd, CancellationToken ct)
    {
        var user = new ApplicationUser
        {
            UserName = cmd.UserName,
            FullName = cmd.FullName,
            Email = cmd.Email,
            MustChangePassword = false,
            IsActive = true,
            EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(user, cmd.Password);
        if (!result.Succeeded)
        {
            var failures = result.Errors.Select(e =>
                new ValidationFailure("Password", e.Description)).ToList();
            throw new AppValidationException(failures);
        }

        await _userManager.AddToRoleAsync(user, cmd.Role);
        return user.Id;
    }
}

public class UpdateUserHandler : IRequestHandler<UpdateUserCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UpdateUserHandler(UserManager<ApplicationUser> userManager)
        => _userManager = userManager;

    public async Task Handle(UpdateUserCommand cmd, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(cmd.UserId.ToString())
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found");

        user.FullName = cmd.FullName;
        user.Email = cmd.Email;

        // Update role — remove all existing, add new
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Any())
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, cmd.NewRole);

        await _userManager.UpdateAsync(user);
    }
}

public class ToggleUserActiveHandler : IRequestHandler<ToggleUserActiveCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ToggleUserActiveHandler(UserManager<ApplicationUser> userManager)
        => _userManager = userManager;

    public async Task Handle(ToggleUserActiveCommand cmd, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(cmd.UserId.ToString())
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found");

        if (cmd.SetActive)
        {
            user.IsActive = true;
            user.LockoutEnd = null;
        }
        else
        {
            user.IsActive = false;
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
        }

        await _userManager.UpdateAsync(user);
    }
}

public class AdminResetPasswordHandler : IRequestHandler<AdminResetPasswordCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminResetPasswordHandler(UserManager<ApplicationUser> userManager)
        => _userManager = userManager;

    public async Task Handle(AdminResetPasswordCommand cmd, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(cmd.UserId.ToString())
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, cmd.NewPassword);

        if (!result.Succeeded)
        {
            var failures = result.Errors.Select(e =>
                new ValidationFailure("NewPassword", e.Description)).ToList();
            throw new AppValidationException(failures);
        }
    }
}

public class UpdateProfileHandler : IRequestHandler<UpdateProfileCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UpdateProfileHandler(UserManager<ApplicationUser> userManager)
        => _userManager = userManager;

    public async Task Handle(UpdateProfileCommand cmd, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(cmd.UserId.ToString())
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found");

        user.FullName = cmd.FullName;
        user.Email = cmd.Email;
        await _userManager.UpdateAsync(user);
    }
}

public class ChangeOwnPasswordHandler : IRequestHandler<ChangeOwnPasswordCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ChangeOwnPasswordHandler(UserManager<ApplicationUser> userManager)
        => _userManager = userManager;

    public async Task Handle(ChangeOwnPasswordCommand cmd, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(cmd.UserId.ToString())
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found");

        var result = await _userManager.ChangePasswordAsync(user, cmd.CurrentPassword, cmd.NewPassword);

        if (!result.Succeeded)
        {
            var failures = result.Errors.Select(e =>
                new ValidationFailure("CurrentPassword", e.Description)).ToList();
            throw new AppValidationException(failures);
        }
    }
}

// ─── Query Handlers ───

public class GetUsersHandler : IRequestHandler<GetUsersQuery, PagedResult<UserListItemDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetUsersHandler(UserManager<ApplicationUser> userManager)
        => _userManager = userManager;

    public async Task<PagedResult<UserListItemDto>> Handle(GetUsersQuery query, CancellationToken ct)
    {
        var totalCount = await _userManager.Users.CountAsync(ct);

        var users = await _userManager.Users
            .OrderBy(u => u.UserName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var items = new List<UserListItemDto>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            items.Add(new UserListItemDto(
                u.Id, u.UserName!, u.FullName, u.Email,
                roles.FirstOrDefault() ?? "", u.IsActive, u.LastLoginAt));
        }

        return new PagedResult<UserListItemDto>(items, totalCount, query.Page, query.PageSize);
    }
}

public class GetAuditLogsHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly MmsDbContext _db;

    public GetAuditLogsHandler(MmsDbContext db) => _db = db;

    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery query, CancellationToken ct)
    {
        var q = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (query.DateFrom.HasValue)
        {
            var from = query.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
            q = q.Where(a => a.Ts >= from);
        }
        if (query.DateTo.HasValue)
        {
            var to = query.DateTo.Value.ToDateTime(TimeOnly.MaxValue);
            q = q.Where(a => a.Ts <= to);
        }
        if (!string.IsNullOrWhiteSpace(query.EntityName))
            q = q.Where(a => a.EntityType == query.EntityName);
        if (!string.IsNullOrWhiteSpace(query.PerformedBy))
            q = q.Where(a => a.Actor.Contains(query.PerformedBy));

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(a => a.Ts)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new AuditLogDto(
                a.Id,
                a.EntityType ?? "",
                a.EntityId.HasValue ? a.EntityId.Value.ToString() : null,
                a.Category.ToString(),
                a.Detail,
                a.Actor,
                a.Ts))
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>(items, totalCount, query.Page, query.PageSize);
    }
}
