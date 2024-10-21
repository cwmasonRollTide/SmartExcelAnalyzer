using Moq;
using Persistence.Cache;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;

namespace SmartExcelAnalyzer.Tests.Persistence.Cache;

public class EmbeddingCacheTests
{
    [Fact]
    public void Constructor_InitializesCache()
    {
        // Arrange
        var options = new MemoryCacheOptions();
        var optionsMock = new Mock<IOptions<MemoryCacheOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);

        // Act
        var cache = new MemoryCacheEmbeddingCache(optionsMock.Object);

        // Assert
        Assert.NotNull(cache);
    }

    [Fact]
    public void GetEmbedding_ReturnsNullForNonExistentKey()
    {
        // Arrange
        var options = new MemoryCacheOptions();
        var optionsMock = new Mock<IOptions<MemoryCacheOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var cache = new MemoryCacheEmbeddingCache(optionsMock.Object);

        // Act
        var result = cache.GetEmbedding("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SetAndGetEmbedding_WorksCorrectly()
    {
        // Arrange
        var options = new MemoryCacheOptions();
        var optionsMock = new Mock<IOptions<MemoryCacheOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var cache = new MemoryCacheEmbeddingCache(optionsMock.Object);
        var text = "test";
        var embedding = new float[] { 1.0f, 2.0f, 3.0f };

        // Act
        cache.SetEmbedding(text, embedding);
        var result = cache.GetEmbedding(text);

        // Assert
        Assert.Equal(embedding, result);
    }

    [Fact]
    public void ClearCache_RemovesAllItems()
    {
        // Arrange
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

        // Act
        cache.ClearCache();

        // Assert
        Assert.Null(cache.GetEmbedding(text1));
        Assert.Null(cache.GetEmbedding(text2));
    }
}