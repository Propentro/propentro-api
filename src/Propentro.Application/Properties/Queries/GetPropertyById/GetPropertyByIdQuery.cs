using Propentro.Application.Common.Interfaces;
using Propentro.Contracts.Properties;

namespace Propentro.Application.Properties.Queries.GetPropertyById;

public sealed record GetPropertyByIdQuery(string PublicId) : IQuery<PropertyDetailDto>;
