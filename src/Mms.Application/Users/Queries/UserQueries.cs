using MediatR;
using Mms.Application.Common.Models;
using Mms.Application.Users.Dtos;

namespace Mms.Application.Users.Queries;

public record GetUsersQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<UserListItemDto>>;

public record GetAuditLogsQuery(
    int Page = 1, int PageSize = 50,
    DateOnly? DateFrom = null, DateOnly? DateTo = null,
    string? EntityName = null, string? PerformedBy = null)
    : IRequest<PagedResult<AuditLogDto>>;
