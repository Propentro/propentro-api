namespace Propentro.Application.Features.Organizations.Members.Commands.InviteMember;

public sealed record InviteMemberCommand(string Email, string RoleId) : ICommand;
