// This file is maintained for backward compatibility.
// New code should use: Propentro.Application.Common.Interfaces.Caching.ICacheService

using Propentro.Application.Common.Interfaces.Caching;

namespace Propentro.Application.Common.Interfaces;

/// <inheritdoc cref="Caching.ICacheService"/>
[Obsolete("Use Propentro.Application.Common.Interfaces.Caching.ICacheService instead.")]
public interface ICacheService : Caching.ICacheService;
