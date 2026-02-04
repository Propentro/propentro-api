using MediatR;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Common.Interfaces.Messaging;

/// <summary>
/// Marker interface for commands that modify state and return a result without data.
/// </summary>
public interface ICommand : IRequest<Result>;

/// <summary>
/// Marker interface for commands that modify state and return a result with data.
/// </summary>
/// <typeparam name="TResponse">The type of data returned on success.</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;

/// <summary>
/// Base interface for command handlers that return a result without data.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;

/// <summary>
/// Base interface for command handlers that return a result with data.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;
