using Propentro.Application.Common.Interfaces;

namespace Propentro.Application.Properties.Commands.DeleteProperty;

public sealed record DeletePropertyCommand(string PublicId) : ICommand;
