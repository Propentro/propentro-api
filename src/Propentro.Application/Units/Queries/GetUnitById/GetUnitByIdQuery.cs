using Propentro.Application.Common.Interfaces;
using Propentro.Contracts.Units;

namespace Propentro.Application.Units.Queries.GetUnitById;

public sealed record GetUnitByIdQuery(string PublicId) : IQuery<UnitDetailDto>;
