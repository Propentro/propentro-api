using Propentro.Application.Common.Interfaces;

namespace Propentro.Application.Leases.Commands.DeleteLease;

public sealed record DeleteLeaseCommand(string PublicId) : ICommand;
