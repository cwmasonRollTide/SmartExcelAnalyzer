using Domain.Persistence.Configuration;

namespace SmartExcelAnalyzer.Tests.Domain.Persistence.Configuration;

public class LLMServiceOptionsTests
{
    [Fact]
    public void LLMServiceOptions_Properties_ShouldBeSettable()
    {
        // Arrange
        var options = new LLMServiceOptions();

        // Act
        options.COMPUTE_BATCH_SIZE = 200;
        options.LLM_SERVICE_URLS = new List<string> { "url1", "url2" };
        options.LLM_SERVICE_URL = "test-url";

        // Assert
        Assert.Equal(200, options.COMPUTE_BATCH_SIZE);
        Assert.Equal(new List<string> { "url1", "url2" }, options.LLM_SERVICE_URLS);
        Assert.Equal("test-url", options.LLM_SERVICE_URL);
    }

    [Fact]
    public void LLMServiceOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new LLMServiceOptions();

        // Assert
        Assert.Equal(100, options.COMPUTE_BATCH_SIZE);
        Assert.Empty(options.LLM_SERVICE_URLS);
        Assert.Equal(string.Empty, options.LLM_SERVICE_URL);
    }
}

public class DatabaseOptionsTests
{
    [Fact]
    public void DatabaseOptions_Properties_ShouldBeSettable()
    {
        // Arrange
        var options = new DatabaseOptions();

        // Act
        options.PORT = 5432;
        options.SAVE_BATCH_SIZE = 1000;
        options.MAX_RETRY_COUNT = 5;
        options.USE_HTTPS = true;
        options.MAX_CONNECTION_COUNT = 20;
        options.HOST = "test-host";
        options.DatabaseName = "test-db";
        options.QDRANT_API_KEY = "test-api-key";
        options.CollectionName = "test-collection";
        options.ConnectionString = "test-connection-string";
        options.CollectionNameTwo = "test-collection-two";

        // Assert
        Assert.Equal(5432, options.PORT);
        Assert.Equal(1000, options.SAVE_BATCH_SIZE);
        Assert.Equal(5, options.MAX_RETRY_COUNT);
        Assert.True(options.USE_HTTPS);
        Assert.Equal(20, options.MAX_CONNECTION_COUNT);
        Assert.Equal("test-host", options.HOST);
        Assert.Equal("test-db", options.DatabaseName);
        Assert.Equal("test-api-key", options.QDRANT_API_KEY);
        Assert.Equal("test-collection", options.CollectionName);
        Assert.Equal("test-connection-string", options.ConnectionString);
        Assert.Equal("test-collection-two", options.CollectionNameTwo);
    }

    [Fact]
    public void DatabaseOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new DatabaseOptions();

        // Assert
        Assert.False(options.USE_HTTPS);
        Assert.Equal(10, options.MAX_CONNECTION_COUNT);
        Assert.Equal(string.Empty, options.HOST);
        Assert.Equal(string.Empty, options.DatabaseName);
        Assert.Equal(string.Empty, options.QDRANT_API_KEY);
        Assert.Equal(string.Empty, options.CollectionName);
        Assert.Equal(string.Empty, options.ConnectionString);
        Assert.Equal(string.Empty, options.CollectionNameTwo);
    }
}