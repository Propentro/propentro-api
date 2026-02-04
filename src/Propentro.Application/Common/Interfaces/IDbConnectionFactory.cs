// This file is maintained for backward compatibility.
// New code should use: Propentro.Application.Common.Interfaces.Persistence.IDbConnectionFactory

using System.Data.Common;

namespace Propentro.Application.Common.Interfaces;

/// <inheritdoc cref="Persistence.IDbConnectionFactory"/>
public interface IDbConnectionFactory : Persistence.IDbConnectionFactory;
