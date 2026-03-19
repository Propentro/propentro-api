namespace Propentro.Contracts.Organizations.Members;

public sealed record MemberRoleResponse
{
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
    public bool IsSystem { get; init; }
}
