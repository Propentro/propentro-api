namespace Propentro.Application.Features.Organizations.Roles.Commands.UpdateRole;

public sealed record UpdateRoleCommand(string RoleId, string? Name, string? Description, string[]? PermissionCodes) : ICommand;
