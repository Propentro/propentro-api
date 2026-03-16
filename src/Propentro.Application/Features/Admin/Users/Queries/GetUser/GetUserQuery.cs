using Propentro.Contracts.Admin.Users;

namespace Propentro.Application.Features.Admin.Users.Queries.GetUser;

public sealed record GetUserQuery(string Id) : IQuery<AdminUserResponse>;
