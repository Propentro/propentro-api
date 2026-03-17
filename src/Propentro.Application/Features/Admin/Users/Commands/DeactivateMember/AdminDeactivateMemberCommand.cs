namespace Propentro.Application.Features.Admin.Users.Commands.DeactivateMember;

public sealed record AdminDeactivateMemberCommand(string OrganizationId, string UserId) : ICommand;
