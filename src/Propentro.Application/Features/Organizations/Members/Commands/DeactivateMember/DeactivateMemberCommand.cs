namespace Propentro.Application.Features.Organizations.Members.Commands.DeactivateMember;

public sealed record DeactivateMemberCommand(string UserId) : ICommand, ICacheInvalidating
{
    public IReadOnlyList<string> CacheKeysToInvalidate { get; } = ["user-permissions:*"];
}
