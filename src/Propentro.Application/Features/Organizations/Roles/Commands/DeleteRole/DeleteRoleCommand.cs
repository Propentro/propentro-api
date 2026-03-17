namespace Propentro.Application.Features.Organizations.Roles.Commands.DeleteRole;

public sealed record DeleteRoleCommand(string RoleId) : ICommand, ICacheInvalidating
{
    public IReadOnlyList<string> CacheKeysToInvalidate { get; } = ["roles-*", "user-permissions:*"];
}
