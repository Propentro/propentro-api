namespace Propentro.Application.Features.Admin.Users.Commands.DeleteUser;

public sealed record DeleteUserCommand(string Id) : ICommand;
