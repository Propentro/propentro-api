using Propentro.Application.Common.Interfaces;
using Propentro.Contracts.Leases;

namespace Propentro.Application.Leases.Queries.GetLeaseById;

public sealed record GetLeaseByIdQuery(string PublicId) : IQuery<LeaseDetailDto>;
