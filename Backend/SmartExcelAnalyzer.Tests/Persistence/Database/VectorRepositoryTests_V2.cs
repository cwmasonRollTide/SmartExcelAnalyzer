using Moq;
using System.Text.Json;
using FluentAssertions;
using Domain.Persistence;
using Persistence.Database;
using Domain.Persistence.DTOs;
using Persistence.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using Domain.Persistence.Configuration;

namespace SmartExcelAnalyzer.Tests.Persistence.Database;

public class VectorRepositoryTests_V2
{
    private const int SAVE_BATCH_SIZE = 10;
    private const int COMPUTE_BATCH_SIZE = 10;

    private readonly Mock<IDatabaseWrapper> _databaseMock = new();
    private readonly Mock<ILLMRepository> _llmRepositoryMock = new();
    private readonly Mock<ILogger<VectorRepository>> _loggerMock = new();
    private readonly Mock<IOptions<LLMServiceOptions>> _llmOptionsMock = new();
    private readonly Mock<IOptions<DatabaseOptions>> _databaseOptionsMock = new();
    private VectorRepository Sut => new(_databaseMock.Object, _loggerMock.Object, _llmRepositoryMock.Object, _llmOptionsMock.Object, _databaseOptionsMock.Object);

    private static readonly float[] singleArray = [1.0f];

    public VectorRepositoryTests_V2()
    {
        _llmOptionsMock.Setup(o => o.Value).Returns(new LLMServiceOptions { LLM_SERVICE_URL = "http://test.com", COMPUTE_BATCH_SIZE = COMPUTE_BATCH_SIZE });
        _databaseOptionsMock.Setup(o => o.Value).Returns(new DatabaseOptions { SAVE_BATCH_SIZE = SAVE_BATCH_SIZE });
    }

    [Fact]
    public async Task SaveDocumentAsync_ShouldSucceedWhenSavingRowsSucceeds_ButSavingSummaryFails_2()
    {
        const string documentId = "1";
        var data = new SummarizedExcelData
        {
            Rows =
            [
                new ConcurrentDictionary<string, object> { ["col1"] = "val1" }
            ],
            Summary = new ConcurrentDictionary<string, object> { ["sum"] = 10 }
        };
        _llmRepositoryMock.Setup(l => l.ComputeBatchEmbeddings(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([[1.0f]]);
        _databaseMock.SetupSequence(c => c.StoreVectorsAsync(It.IsAny<ConcurrentBag<ConcurrentDictionary<string, object>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentId);

        _databaseMock.Setup(c => c.StoreSummaryAsync(It.Is<string>(y => y == documentId), It.IsAny<ConcurrentDictionary<string, object>>(), It.IsAny<CancellationToken>())).ReturnsAsync(-1);

        var result = await Sut.SaveDocumentAsync(data);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveDocumentAsync_ShouldReturnNullWhenInputDataIsNull_2()
    {
        var result = await Sut.SaveDocumentAsync(null!);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveDocumentAsync_ShouldReturnNullWhenInputDataIsEmpty_2()
    {
        (await Sut.SaveDocumentAsync(null!)).Should().BeNull();
    }

    [Fact]
    public async Task QueryVectorData_ShouldReturnRelevantRowsAndSummary_2()
    {
        var documentId = "testDoc";
        var queryVector = new float[] { 1.0f, 2.0f, 3.0f };
        var documents = new List<Document>
        {
            new() { Id = documentId, Content = "{\"col1\":\"val1\"}", Embedding = [1.0f, 2.0f, 3.0f] }
        };
        var summaries = new List<Summary>
        {
            new() { Id = documentId, Content = "{\"sum\":10}" }
        };
        _databaseMock.Setup(c => c.GetRelevantDocumentsAsync(It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents.Select(d => new ConcurrentDictionary<string, object>
            {
                ["content"] = JsonSerializer.Deserialize<ConcurrentDictionary<string, object>>(d.Content)!
            }));
        var contentDict = JsonSerializer.Deserialize<ConcurrentDictionary<string, object>>(summaries.First().Content)!;
        _databaseMock.Setup(c => c.GetSummaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConcurrentDictionary<string, object>
            {
                ["content"] = contentDict
            });
        var result = await Sut.QueryVectorDataAsync(documentId, queryVector);

        result.Should().NotBeNull();
        result.Rows.Should().HaveCount(1);
        result.Rows!.First().Should().ContainKey("content");
        result.Summary!["content"].ToString().Should().BeEquivalentTo(contentDict.ToString());
    }

    [Fact]
    public async Task QueryVectorData_ShouldReturnNullWhenNoRelevantDocumentsFound_2()
    {
        var documentId = "testDoc";
        var queryVector = new float[] { 1.0f, 2.0f, 3.0f };
        _databaseMock.Setup(c => c.GetRelevantDocumentsAsync(It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await Sut.QueryVectorDataAsync(documentId, queryVector);

        result.Should().BeNull();
    }

    [Fact]
    public async Task QueryVectorData_ShouldReturnRowsWithoutSummaryWhenSummaryDoesNotExist_2()
    {
        var documentId = "testDoc";
        var queryVector = new float[] { 1.0f, 2.0f, 3.0f };
        var documents = new List<Document>
        {
            new() { Id = documentId, Content = "{\"col1\":\"val1\"}", Embedding = [1.0f, 2.0f, 3.0f] }
        };
        _databaseMock.Setup(c => c.GetRelevantDocumentsAsync(It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents.Select(d => new ConcurrentDictionary<string, object>
            {
                ["content"] = JsonSerializer.Deserialize<ConcurrentDictionary<string, object>>(d.Content)!
            }));
        _databaseMock.Setup(c => c.GetSummaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConcurrentDictionary<string, object>)null!);

        var result = await Sut.QueryVectorDataAsync(documentId, queryVector);

        result.Should().NotBeNull();
        result.Rows.Should().HaveCount(1);
        result.Rows!.First().Should().ContainKey("content");
        result.Summary.Should().BeNull();
    }

    [Fact]
    public async Task QueryVectorData_ShouldReturnNullWhenDocumentIdIsNullOrEmpty_2()
    {
        var queryVector = new float[] { 1.0f, 2.0f, 3.0f };

        var result = await Sut.QueryVectorDataAsync(string.Empty, queryVector);

        result.Should().BeNull();
    }

    [Fact]
    public async Task QueryVectorData_ShouldReturnNullWhenQueryVectorIsNull_2()
    {
        var documentId = "testDoc";

        var result = await Sut.QueryVectorDataAsync(documentId, null!);

        result.Should().BeNull();
    }

    [Fact]
    public async Task QueryVectorData_ShouldReturnNullWhenQueryVectorIsEmpty_2()
    {
        var documentId = "testDoc";
        var emptyQueryVector = Array.Empty<float>();

        var result = await Sut.QueryVectorDataAsync(documentId, emptyQueryVector);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveDocumentAsync_ShouldHandleExceptionAndReturnNull_2()
    {
        var data = new SummarizedExcelData
        {
            Rows =
            [
                new ConcurrentDictionary<string, object> { ["col1"] = "val1" }
            ],
            Summary = new ConcurrentDictionary<string, object> { ["sum"] = 10 }
        };
        _databaseMock.Setup(c => c.StoreVectorsAsync(It.IsAny<ConcurrentBag<ConcurrentDictionary<string, object>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => null!);
        _llmRepositoryMock.Setup(l => l.ComputeBatchEmbeddings(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        var result = await Sut.SaveDocumentAsync(data);

        result.Should().BeNullOrEmpty();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error computing embeddings")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task QueryVectorData_ShouldHandleExceptionAndReturnNull_2()
    {
        var documentId = "testDoc";
        var queryVector = new float[] { 1.0f, 2.0f, 3.0f };
        _databaseMock.Setup(c => c.GetRelevantDocumentsAsync(It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        var result = await Sut.QueryVectorDataAsync(documentId, queryVector);

        result.Should().BeNull();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("An error occurred while querying vector data for document")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveDocumentAsync_ShouldReturnNullWhenRowsAreEmpty_2()
    {
        var data = new SummarizedExcelData
        {
            Rows = [],
            Summary = new ConcurrentDictionary<string, object> { ["sum"] = 10 }
        };

        var result = await Sut.SaveDocumentAsync(data);

        result.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task SaveDocumentAsync_ShouldSucceedWhenSummaryIsNull_2()
    {
        const string documentId = "1";
        var data = new SummarizedExcelData
        {
            Rows =
            [
                new ConcurrentDictionary<string, object> { ["col1"] = "val1" }
            ],
            Summary = null
        };
        _llmRepositoryMock.Setup(l => l.ComputeBatchEmbeddings(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([[1.0f]]);
        _databaseMock.Setup(c => c.StoreVectorsAsync(It.IsAny<ConcurrentBag<ConcurrentDictionary<string, object>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentId);

        var result = await Sut.SaveDocumentAsync(data);

        result.Should().Be(documentId);
    }
}


