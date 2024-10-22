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

namespace SmartExcelAnalyzer.Tests.Persistence;

public class VectorDbRepositoryTests
{
    private const int SAVE_BATCH_SIZE = 10;
    private const int COMPUTE_BATCH_SIZE = 10;

    private readonly Mock<IDatabaseWrapper> _databaseMock = new();
    private readonly Mock<ILLMRepository> _llmRepositoryMock = new();
    private readonly Mock<ILogger<VectorRepository>> _loggerMock = new();
    private readonly Mock<IOptions<LLMServiceOptions>> _llmOptionsMock = new();
    private readonly Mock<IOptions<DatabaseOptions>> _databaseOptionsMock = new();
    private VectorRepository Sut => new(_databaseMock.Object, _loggerMock.Object, _llmRepositoryMock.Object, _llmOptionsMock.Object, _databaseOptionsMock.Object);

    private static readonly float[] singleArray = new float[] { 1.0f };

    public VectorDbRepositoryTests()
    {
        _llmOptionsMock.Setup(o => o.Value).Returns(new LLMServiceOptions { LLM_SERVICE_URL = "http://test.com", COMPUTE_BATCH_SIZE = COMPUTE_BATCH_SIZE });
        _databaseOptionsMock.Setup(o => o.Value).Returns(new DatabaseOptions { SAVE_BATCH_SIZE = SAVE_BATCH_SIZE });
    }

    [Fact]
    public async Task SaveDocumentAsync_ShouldSucceedWhenSavingRowsSucceeds_ButSavingSummaryFails()
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
    public async Task SaveDocumentAsync_ShouldReturnNullWhenInputDataIsNull()
    {
        var result = await Sut.SaveDocumentAsync(null!);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveDocumentAsync_ShouldReturnNullWhenInputDataIsEmpty()
    {
        (await Sut.SaveDocumentAsync(null!)).Should().BeNull();
    }

    [Fact]
    public async Task QueryVectorData_ShouldReturnRelevantRowsAndSummary()
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
        var result = await Sut.QueryVectorData(documentId, queryVector);

        result.Should().NotBeNull();
        result.Rows.Should().HaveCount(1);
        result.Rows!.First().Should().ContainKey("content");
        result.Summary!["content"].ToString().Should().BeEquivalentTo(contentDict.ToString());
    }

    [Fact]
    public async Task QueryVectorData_ShouldReturnNullWhenNoRelevantDocumentsFound()
    {
        var documentId = "testDoc";
        var queryVector = new float[] { 1.0f, 2.0f, 3.0f };
        _databaseMock.Setup(c => c.GetRelevantDocumentsAsync(It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await Sut.QueryVectorData(documentId, queryVector);

        result.Should().BeNull();
    }

    [Fact]
    public async Task QueryVectorData_ShouldReturnRowsWithoutSummaryWhenSummaryDoesNotExist()
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

        var result = await Sut.QueryVectorData(documentId, queryVector);

        result.Should().NotBeNull();
        result.Rows.Should().HaveCount(1);
        result.Rows!.First().Should().ContainKey("content");
        result.Summary.Should().BeNull();
    }

    [Fact]
    public async Task QueryVectorData_ShouldReturnNullWhenDocumentIdIsNullOrEmpty()
    {
        var queryVector = new float[] { 1.0f, 2.0f, 3.0f };

        var result = await Sut.QueryVectorData(string.Empty, queryVector);

        result.Should().BeNull();
    }

    [Fact]
    public async Task QueryVectorData_ShouldReturnNullWhenQueryVectorIsNull()
    {
        var documentId = "testDoc";

        var result = await Sut.QueryVectorData(documentId, null!);

        result.Should().BeNull();
    }

    [Fact]
    public async Task QueryVectorData_ShouldReturnNullWhenQueryVectorIsEmpty()
    {
        var documentId = "testDoc";
        var emptyQueryVector = Array.Empty<float>();

        var result = await Sut.QueryVectorData(documentId, emptyQueryVector);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveDocumentAsync_ShouldHandleExceptionAndReturnNull()
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
    public async Task QueryVectorData_ShouldHandleExceptionAndReturnNull()
    {
        var documentId = "testDoc";
        var queryVector = new float[] { 1.0f, 2.0f, 3.0f };
        _databaseMock.Setup(c => c.GetRelevantDocumentsAsync(It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        var result = await Sut.QueryVectorData(documentId, queryVector);

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
    public async Task SaveDocumentAsync_ShouldReturnNullWhenRowsAreEmpty()
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
    public async Task SaveDocumentAsync_ShouldSucceedWhenSummaryIsNull()
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

    [Fact]
    public async Task SaveDocumentAsync_ShouldCancelOperationWhenCancellationRequested()
    {
        var data = new SummarizedExcelData
        {
            Rows =
            [
                new ConcurrentDictionary<string, object> { ["col1"] = "val1" }
            ],
            Summary = new ConcurrentDictionary<string, object> { ["sum"] = 10 }
        };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await Sut.SaveDocumentAsync(data, cancellationToken: cts.Token);

        result.Should().BeNullOrEmpty();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Batch creation was cancelled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveDocumentAsync_ShouldReportProgressCorrectly()
    {
        const string documentId = "1";
        var data = new SummarizedExcelData
        {
            Rows = new ConcurrentBag<ConcurrentDictionary<string, object>>(Enumerable.Range(0, 100).Select(i => new ConcurrentDictionary<string, object> { ["col1"] = $"val{i}" })),
            Summary = new ConcurrentDictionary<string, object> { ["sum"] = 10 }
        };
        _llmRepositoryMock.Setup(l => l.ComputeBatchEmbeddings(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Range(0, 10).Select(_ => singleArray).ToArray());
        _databaseMock.Setup(c => c.StoreVectorsAsync(It.IsAny<ConcurrentBag<ConcurrentDictionary<string, object>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentId);

        var progressReports = new List<(double, double)>();
        var progress = new Progress<(double, double)>(progressReports.Add);

        await Sut.SaveDocumentAsync(data, progress);

        progressReports.Should().NotBeEmpty();
        progressReports.Last().Should().Be((1, 1)); // Final progress should be (1, 1)
    }

    [Fact]
    public async Task QueryVectorData_ShouldHandleExceptionInGetSummaryAsync()
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

        var result = await Sut.QueryVectorData(documentId, queryVector);

        result.Should().NotBeNull();
        result.Rows.Should().HaveCount(1);
        result.Summary.Should().BeNull();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Failed to query the summary of the document")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveDocumentAsync_ShouldHandleInconsistentDocumentIds()
    {
        var data = new SummarizedExcelData
        {
            Rows = new ConcurrentBag<ConcurrentDictionary<string, object>>(Enumerable.Range(0, 20).Select(i => new ConcurrentDictionary<string, object> { ["col1"] = $"val{i}" })),
            Summary = new ConcurrentDictionary<string, object> { ["sum"] = 10 }
        };
        _llmRepositoryMock.Setup(l => l.ComputeBatchEmbeddings(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Range(0, 10).Select(_ => new float[] { 1.0f }).ToArray());
        _databaseMock.SetupSequence(c => c.StoreVectorsAsync(It.IsAny<ConcurrentBag<ConcurrentDictionary<string, object>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("1")
            .ReturnsAsync("2");

        var result = await Sut.SaveDocumentAsync(data);

        result.Should().Be("1");
        // _loggerMock.Verify(
        //     x => x.Log(
        //         LogLevel.Warning,
        //         It.IsAny<EventId>(),
        //         It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Inconsistent document IDs across batches")),
        //         It.IsAny<Exception>(),
        //         It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        //     Times.Once);
    }

    [Fact]
    public async Task SaveDocumentAsync_ShouldHandleNullEmbeddings()
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
            .ReturnsAsync(new float[]?[] { null });
        _databaseMock.Setup(c => c.StoreVectorsAsync(It.IsAny<ConcurrentBag<ConcurrentDictionary<string, object>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentId);

        var result = await Sut.SaveDocumentAsync(data);

        result.Should().Be(documentId);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Embedding at index")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task QueryVectorData_ShouldReturnMaximumNumberOfRelevantDocuments()
    {
        var documentId = "testDoc";
        var queryVector = new float[] { 1.0f, 2.0f, 3.0f };
        var maxRelevantCount = 20;
        var documents = Enumerable.Range(0, maxRelevantCount).Select(i => new Document
        {
            Id = documentId,
            Content = $"{{\"col1\":\"val{i}\"}}",
            Embedding = [1.0f, 2.0f, 3.0f]
        }).ToList();

        _databaseMock.Setup(c => c.GetRelevantDocumentsAsync(It.IsAny<string>(), It.IsAny<float[]>(), It.Is<int>(x => x == maxRelevantCount), It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents.Select(d => new ConcurrentDictionary<string, object>
            {
                ["content"] = JsonSerializer.Deserialize<ConcurrentDictionary<string, object>>(d.Content)!
            }));
        _databaseMock.Setup(c => c.GetSummaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConcurrentDictionary<string, object> { ["sum"] = 10 });

        var result = await Sut.QueryVectorData(documentId, queryVector, maxRelevantCount);

        result.Should().NotBeNull();
        result.Rows.Should().HaveCount(maxRelevantCount);
    }
}
