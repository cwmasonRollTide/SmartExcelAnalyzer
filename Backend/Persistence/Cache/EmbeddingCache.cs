using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;

namespace Persistence.Cache;

public interface IEmbeddingCache
{
    float[]? GetEmbedding(string text);
    void SetEmbedding(string text, float[] embedding);
}

public class MemoryCacheEmbeddingCache : IEmbeddingCache
{
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public MemoryCacheEmbeddingCache(IOptions<MemoryCacheOptions> options)
    {
        var cacheOptions = new MemoryCacheOptions
        {
            SizeLimit = options.Value.SizeLimit ?? 10_000_000
        };
        _cache = new MemoryCache(cacheOptions);
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetSize(1)
            .SetPriority(CacheItemPriority.Normal);
    }

    public float[]? GetEmbedding(string text) => _cache.Get<float[]>(text);

    public void SetEmbedding(string text, float[] embedding)
    {
        _cache.Set(
            text, 
            embedding, 
            _cacheOptions
        );
    }

    public void ClearCache()
    {
        if (_cache is MemoryCache memoryCache)
            memoryCache.Compact(1.0);
    }
}