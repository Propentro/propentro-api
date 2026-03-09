namespace Propentro.Application.Features.Organizations.Roles.Commands.CreateRole;

public sealed record CreateRoleCommand(string Name, string? Description, string[] PermissionCodes) : ICommand<string>;
