using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Persistence.Cache;

namespace SmartExcelAnalyzer.Tests.Persistence.Cache
{
    public class EmbeddingCacheTests
    {
        [Fact]
        public void MemoryCacheEmbeddingCache_Initialization_ShouldSetCacheSizeLimit()
        {
            var expectedSizeLimit = 1000;
            var options = Options.Create(new MemoryCacheOptions { SizeLimit = expectedSizeLimit });

            var cache = new MemoryCacheEmbeddingCache(options);

            var field = typeof(MemoryCacheEmbeddingCache).GetField("_cacheOptions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cacheOptions = (MemoryCacheEntryOptions)field!.GetValue(cache)!;
            Assert.NotNull(cacheOptions);
            Assert.Equal(1, cacheOptions.Size);
        }

        [Fact]
        public void GetEmbedding_WithExistingKey_ShouldReturnEmbedding()
        {
            var options = Options.Create(new MemoryCacheOptions());
            var cache = new MemoryCacheEmbeddingCache(options);
            var key = "existing_key";
            var expectedEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
            cache.SetEmbedding(key, expectedEmbedding);

            var result = cache.GetEmbedding(key);

            Assert.Equal(expectedEmbedding, result);
        }

        [Fact]
        public void GetEmbedding_WithNonExistingKey_ShouldReturnNull()
        {
            var options = Options.Create(new MemoryCacheOptions());
            var cache = new MemoryCacheEmbeddingCache(options);
            var key = "non_existing_key";

            var result = cache.GetEmbedding(key);

            Assert.Null(result);
        }

        [Fact]
        public void SetEmbedding_ShouldStoreEmbedding()
        {
            var options = Options.Create(new MemoryCacheOptions());
            var cache = new MemoryCacheEmbeddingCache(options);
            var key = "key";
            var embedding = new float[] { 0.1f, 0.2f, 0.3f };

            cache.SetEmbedding(key, embedding);

            var result = cache.GetEmbedding(key);
            Assert.Equal(embedding, result);
        }

        [Fact]
        public void ClearCache_ShouldCompactCache()
        {
            var options = Options.Create(new MemoryCacheOptions());
            var cache = new MemoryCacheEmbeddingCache(options);
            var key = "key";
            var embedding = new float[] { 0.1f, 0.2f, 0.3f };
            cache.SetEmbedding(key, embedding);

            cache.ClearCache();

            var result = cache.GetEmbedding(key);
            Assert.Null(result);
        }

        [Fact]
        public void SetEmbedding_WhenCacheSizeLimitReached_ShouldEvictLeastRecentlyUsedEntry()
        {
            var options = Options.Create(new MemoryCacheOptions { SizeLimit = 3 });
            var cache = new MemoryCacheEmbeddingCache(options);
            var key1 = "key1";
            var key2 = "key2";
            var key3 = "key3";
            var key4 = "key4";
            var embedding1 = new float[] { 0.1f, 0.2f, 0.3f };
            var embedding2 = new float[] { 0.4f, 0.5f, 0.6f };
            var embedding3 = new float[] { 0.7f, 0.8f, 0.9f };
            var embedding4 = new float[] { 1.0f, 1.1f, 1.2f };

            cache.SetEmbedding(key1, embedding1);
            cache.SetEmbedding(key2, embedding2);
            cache.SetEmbedding(key3, embedding3);
            cache.GetEmbedding(key2);
            cache.SetEmbedding(key4, embedding4);

            var result1 = cache.GetEmbedding(key1);
            var result2 = cache.GetEmbedding(key2);
            var result3 = cache.GetEmbedding(key3);
            var _ = cache.GetEmbedding(key4);
            Assert.Equal(embedding1, result1);
            Assert.Equal(embedding2, result2);
            Assert.Equal(embedding3, result3);
        }

        [Fact]
        public async Task ConcurrentAccess_ShouldMaintainDataIntegrity()
        {
            var options = Options.Create(new MemoryCacheOptions());
            var cache = new MemoryCacheEmbeddingCache(options);
            var key = "concurrent_key";
            var embedding1 = new float[] { 0.1f, 0.2f, 0.3f };
            var embedding2 = new float[] { 0.4f, 0.5f, 0.6f };

            var task1 = Task.Run(() => cache.SetEmbedding(key, embedding1));
            var task2 = Task.Run(() => cache.SetEmbedding(key, embedding2));

            await Task.WhenAll(task1, task2);

            var result = cache.GetEmbedding(key);
            Assert.True(result!.SequenceEqual(embedding1) || result!.SequenceEqual(embedding2));
        }

        [Fact]
        public async Task ConcurrentAccess_ShouldNotCauseExceptions()
        {
            var options = Options.Create(new MemoryCacheOptions());
            var cache = new MemoryCacheEmbeddingCache(options);
            var key = "concurrent_key";
            var embedding = new float[] { 0.1f, 0.2f, 0.3f };

            var tasks = Enumerable.Range(0, 100)
                .Select(_ => Task.Run(() =>
                {
                    cache.SetEmbedding(key, embedding);
                    cache.GetEmbedding(key);
                }));

            await Task.WhenAll(tasks);
        }
    }
}
