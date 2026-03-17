using Propentro.Contracts.Organizations.Invites;

namespace Propentro.Application.Features.Organizations.Invites.Commands.InviteToCurrentOrg;

public sealed record InviteToCurrentOrgCommand(
    string Email,
    string RoleId,
    Guid OrganizationId,
    Guid InvitedByUserId) : ICommand<InviteResult>;
