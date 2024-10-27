using Domain.Persistence.Configuration;

namespace SmartExcelAnalyzer.Tests.Domain.Persistence.Configuration;

public class LLMServiceOptionsTests
{
    [Fact]
    public void LLMServiceOptions_Properties_ShouldBeSettable()
    {
        var options = new LLMServiceOptions
        {
            COMPUTE_BATCH_SIZE = 200,
            LLM_SERVICE_URLS = ["url1", "url2"],
            LLM_SERVICE_URL = "test-url"
        };

        Assert.Equal(200, options.COMPUTE_BATCH_SIZE);
        Assert.Equal(new List<string> { "url1", "url2" }, options.LLM_SERVICE_URLS);
        Assert.Equal("test-url", options.LLM_SERVICE_URL);
    }

    [Fact]
    public void LLMServiceOptions_DefaultValues_ShouldBeCorrect()
    { 
        var options = new LLMServiceOptions();

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
        var options = new DatabaseOptions
        {
            PORT = 5432,
            SAVE_BATCH_SIZE = 1000,
            MAX_RETRY_COUNT = 5,
            USE_HTTPS = true,
            MAX_CONNECTION_COUNT = 20,
            HOST = "test-host",
            DatabaseName = "test-db",
            QDRANT_API_KEY = "test-api-key",
            CollectionName = "test-collection",
            ConnectionString = "test-connection-string",
            CollectionNameTwo = "test-collection-two"
        };

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
        var options = new DatabaseOptions();

        Assert.False(options.USE_HTTPS);
        Assert.Equal(10, options.MAX_CONNECTION_COUNT);
        Assert.Equal(" ", options.HOST);
        Assert.Equal(" ", options.DatabaseName);
        Assert.Equal(" ", options.QDRANT_API_KEY);
        Assert.Equal(" ", options.CollectionName);
        Assert.Equal(" ", options.ConnectionString);
        Assert.Equal(" ", options.CollectionNameTwo);
    }
}