namespace Propentro.Application.Features.Admin.Users.Commands.ReactivateUser;

public sealed record ReactivateUserCommand(string PublicId) : ICommand;
