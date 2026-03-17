namespace Propentro.Application.Features.Admin.Users.Commands.ReactivateMember;

public sealed record AdminReactivateMemberCommand(string OrganizationId, string UserId) : ICommand;
