using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Contracts.Me;

namespace Propentro.Application.Me.Queries.GetMe;

public sealed record GetMeQuery : IQuery<MeDto>;
