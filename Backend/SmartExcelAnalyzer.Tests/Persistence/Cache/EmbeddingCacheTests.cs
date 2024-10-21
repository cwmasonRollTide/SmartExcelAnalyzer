using Moq;
using FluentAssertions;
using Persistence.Cache;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;

namespace SmartExcelAnalyzer.Tests.Persistence.Cache;

public class EmbeddingCacheTests
{
    [Fact]
    public void Constructor_InitializesCache()
    {
        var options = new MemoryCacheOptions();
        var optionsMock = new Mock<IOptions<MemoryCacheOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);

        var cache = new MemoryCacheEmbeddingCache(optionsMock.Object);

        cache.Should().NotBeNull();
    }

    [Fact]
    public void GetEmbedding_ReturnsNullForNonExistentKey()
    {
        var options = new MemoryCacheOptions();
        var optionsMock = new Mock<IOptions<MemoryCacheOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var cache = new MemoryCacheEmbeddingCache(optionsMock.Object);

        var result = cache.GetEmbedding("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public void SetAndGetEmbedding_WorksCorrectly()
    {
        var options = new MemoryCacheOptions();
        var optionsMock = new Mock<IOptions<MemoryCacheOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var cache = new MemoryCacheEmbeddingCache(optionsMock.Object);
        var text = "test";
        var embedding = new float[] { 1.0f, 2.0f, 3.0f };

        cache.SetEmbedding(text, embedding);
        var result = cache.GetEmbedding(text);

        result.Should().BeEquivalentTo(embedding);
    }

    [Fact]
    public void ClearCache_RemovesAllItems()
    {
        var options = new MemoryCacheOptions();
        var optionsMock = new Mock<IOptions<MemoryCacheOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var cache = new MemoryCacheEmbeddingCache(optionsMock.Object);
        var text1 = "test1";
        var text2 = "test2";
        var embedding1 = new float[] { 1.0f, 2.0f, 3.0f };
        var embedding2 = new float[] { 4.0f, 5.0f, 6.0f };

        cache.SetEmbedding(text1, embedding1);
        cache.SetEmbedding(text2, embedding2);

        cache.ClearCache();

        cache.GetEmbedding(text1).Should().BeNull();
        cache.GetEmbedding(text2).Should().BeNull();
    }
}