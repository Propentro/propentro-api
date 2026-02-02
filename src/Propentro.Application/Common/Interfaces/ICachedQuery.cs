namespace Propentro.Application.Common.Interfaces;

public interface ICachedQuery
{
    string CacheKey { get; }
    TimeSpan? Expiration { get; }
}

public interface ICachedQuery<TResponse> : IQuery<TResponse>, ICachedQuery;
