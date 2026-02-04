using Propentro.Application.Common.Interfaces;

namespace Propentro.Infrastructure.Caching;

public sealed class MemoryCacheService() : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
