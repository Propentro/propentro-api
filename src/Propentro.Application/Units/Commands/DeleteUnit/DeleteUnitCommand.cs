
namespace Propentro.Application.Units.Commands.DeleteUnit;

public sealed record DeleteUnitCommand(string PublicId) : ICommand;
