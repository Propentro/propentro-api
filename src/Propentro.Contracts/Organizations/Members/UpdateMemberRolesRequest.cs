namespace Propentro.Contracts.Organizations.Members;

public sealed record UpdateMemberRolesRequest
{
    public string[] RoleIds { get; init; } = [];
}
