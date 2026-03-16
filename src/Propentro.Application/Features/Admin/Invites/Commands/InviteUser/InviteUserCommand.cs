namespace Propentro.Application.Features.Admin.Invites.Commands.InviteUser;

public sealed record InviteUserCommand(
    string OrganizationId,
    string Email,
    string RolePublicId,
    Guid InvitedByUserId) : ICommand;
