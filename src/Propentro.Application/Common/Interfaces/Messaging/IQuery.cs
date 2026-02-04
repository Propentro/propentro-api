using MediatR;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Common.Interfaces.Messaging;

/// <summary>
/// Marker interface for queries that read data without modifying state.
/// </summary>
/// <typeparam name="TResponse">The type of data returned.</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;

/// <summary>
/// Base interface for query handlers.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
