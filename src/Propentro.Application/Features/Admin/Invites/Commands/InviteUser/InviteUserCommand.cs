using Propentro.Contracts.Organizations.Invites;

namespace Propentro.Application.Features.Admin.Invites.Commands.InviteUser;

public sealed record InviteUserCommand(
    string OrganizationId,
    string Email,
    string RoleId,
    Guid InvitedByUserId) : ICommand<InviteResult>;
