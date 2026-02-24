namespace Propentro.Infrastructure.Caching;

public sealed class CachingOptions
{
    public const string SectionName = "Caching";

    public string KeyPrefix { get; init; } = "propentro:";

    public TimeSpan DefaultExpiration { get; init; } = TimeSpan.FromMinutes(5);
}
