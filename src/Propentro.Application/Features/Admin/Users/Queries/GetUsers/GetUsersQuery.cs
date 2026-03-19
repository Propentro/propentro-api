using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Users;

namespace Propentro.Application.Features.Admin.Users.Queries.GetUsers;

public sealed record GetUsersQuery(
    string? Search,
    string? Status,
    int Page = 1,
    int PageSize = 25) : IQuery<PagedResult<AdminUserListResponse>>;
