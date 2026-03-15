namespace Propentro.Application.Features.Admin.Users.Commands.DeactivateUser;

public sealed record DeactivateUserCommand(string PublicId) : ICommand;
