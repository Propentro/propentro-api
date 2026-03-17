namespace Propentro.Application.Features.Organizations.Invites.Commands.CancelInvite;

public sealed record CancelInviteCommand(
    string Id,
    Guid CanceledByUserId,
    Guid? OrganizationId = null) : ICommand;
