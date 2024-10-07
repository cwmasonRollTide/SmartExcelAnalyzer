using System.Collections.Concurrent;
using System.Text.Json;
using Domain.Persistence;
using Domain.Persistence.DTOs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Persistence.Repositories;
using SmartExcelAnalyzer.Tests.TestUtilities;

namespace SmartExcelAnalyzer.Tests.Persistence;

public class VectorDbRepositoryTests
{
    private readonly Mock<IDatabaseWrapper> _databaseMock = new();
    private readonly Mock<ILLMRepository> _llmRepositoryMock = new();
    private readonly Mock<ILogger<VectorDbRepository>> _loggerMock = new();
    private VectorDbRepository Sut => new(_databaseMock.Object, _loggerMock.Object);

    [Fact]
    public async Task SaveDocumentAsync_ShouldSaveDocumentAndSummary()
    {
        var data = new SummarizedExcelData
        {
            Rows =
            [
                new ConcurrentDictionary<string, object> { ["col1"] = "val1" }
            ],
            Summary = new ConcurrentDictionary<string, object> { ["sum"] = 10 }
        };
        _llmRepositoryMock.Setup(l => l.ComputeEmbedding(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([1.0f]);
        _databaseMock.Setup(c => c.StoreVectorsAsync(It.IsAny<ConcurrentBag<ConcurrentDictionary<string, object>>>(), It.IsAny<CancellationToken>())).ReturnsAsync("1");
        _databaseMock.Setup(c => c.StoreSummaryAsync(It.IsAny<string>(), It.IsAny<ConcurrentDictionary<string, object>>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await Sut.SaveDocumentAsync(data);

        result.Should().NotBeNullOrEmpty();
        result.Should().BeEquivalentTo("1");
    }

    [Fact]
    public async Task SaveDocumentAsync_ShouldReturnNullWhenSavingDocumentsFails()
    {
        var data = new SummarizedExcelData
        {
            Rows =
            [
                new ConcurrentDictionary<string, object> { ["col1"] = "val1" }
            ],
            Summary = new ConcurrentDictionary<string, object> { ["sum"] = 10 }
        };
        _llmRepositoryMock.Setup(l => l.ComputeEmbedding(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([1.0f]);
        _databaseMock.Setup(c => c.StoreVectorsAsync(It.IsAny<ConcurrentBag<ConcurrentDictionary<string, object>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((string)null!);

        var result = await Sut.SaveDocumentAsync(data);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveDocumentAsync_ShouldShortCircuitWhenSavingRowsSucceeds_ButSavingSummaryFails()
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
        _llmRepositoryMock.Setup(l => l.ComputeEmbedding(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([1.0f]);
        _databaseMock.SetupSequence(c => c.StoreVectorsAsync(It.IsAny<ConcurrentBag<ConcurrentDictionary<string, object>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentId);

        _databaseMock.Setup(c => c.StoreSummaryAsync(It.Is<string>(y => y == documentId), It.IsAny<ConcurrentDictionary<string, object>>(), It.IsAny<CancellationToken>())).ReturnsAsync(-1);

        var result = await Sut.SaveDocumentAsync(data);

        result.Should().Be(documentId);
        _loggerMock.VerifyLog(LogLevel.Warning, $"Failed to save the summary of the document with Id {documentId} to the database.");
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
}