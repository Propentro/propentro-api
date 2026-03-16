namespace Propentro.Application.Features.Admin.Invites.Commands.CancelInvite;

public sealed record CancelInviteCommand(
    string Id,
    Guid CancelledByUserId,
    Guid? OrganizationId = null) : ICommand;
