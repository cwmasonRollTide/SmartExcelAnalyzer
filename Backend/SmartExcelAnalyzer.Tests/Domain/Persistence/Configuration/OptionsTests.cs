using FluentAssertions;
using Domain.Persistence.Configuration;

namespace SmartExcelAnalyzer.Tests.Domain.Persistence.Configuration;

public class LLMServiceOptionsTests
{
    [Fact]
    public void LLMServiceOptions_Properties_ShouldBeSettable()
    {
        var serviceUrl = "test-url";
        var computeBatchSize = 200;
        var serviceUrls = new List<string> { "url1", "url2" };
        var options = new LLMServiceOptions
        {
            COMPUTE_BATCH_SIZE = computeBatchSize,
            LLM_SERVICE_URLS = serviceUrls,
            LLM_SERVICE_URL = serviceUrl
        };

        options.LLM_SERVICE_URL.Should().Be(serviceUrl);
        options.LLM_SERVICE_URLS.Should().BeEquivalentTo(serviceUrls);
        options.COMPUTE_BATCH_SIZE.Should().Be(computeBatchSize);
    }

    [Fact]
    public void LLMServiceOptions_DefaultValues_ShouldBeCorrect()
    { 
        var options = new LLMServiceOptions();

        options.COMPUTE_BATCH_SIZE.Should().Be(100);
        options.LLM_SERVICE_URLS.Should().BeEmpty();
        options.LLM_SERVICE_URL.Should().BeEmpty();
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

        options.PORT.Should().Be(5432);
        options.SAVE_BATCH_SIZE.Should().Be(1000);
        options.MAX_RETRY_COUNT.Should().Be(5);
        options.USE_HTTPS.Should().BeTrue();
        options.MAX_CONNECTION_COUNT.Should().Be(20);
        options.HOST.Should().Be("test-host");
        options.DatabaseName.Should().Be("test-db");
        options.QDRANT_API_KEY.Should().Be("test-api-key");
        options.CollectionName.Should().Be("test-collection");
        options.ConnectionString.Should().Be("test-connection-string");
        options.CollectionNameTwo.Should().Be("test-collection-two");
    }

    [Fact]
    public void DatabaseOptions_DefaultValues_ShouldBeCorrect()
    { 
        var options = new DatabaseOptions();

        options.USE_HTTPS.Should().BeFalse();
        options.MAX_CONNECTION_COUNT.Should().Be(10);
        options.HOST.Should().Be(" ");
        options.DatabaseName.Should().Be(" ");
        options.QDRANT_API_KEY.Should().Be(" ");
        options.CollectionName.Should().Be(" ");
        options.ConnectionString.Should().Be(" ");
        options.CollectionNameTwo.Should().Be(" ");
    }
}