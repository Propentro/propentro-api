namespace Propentro.Application.Features.Organizations.Members.Commands.UpdateMemberRoles;

public sealed record UpdateMemberRolesCommand(string UserId, string[] RoleIds) : ICommand;
