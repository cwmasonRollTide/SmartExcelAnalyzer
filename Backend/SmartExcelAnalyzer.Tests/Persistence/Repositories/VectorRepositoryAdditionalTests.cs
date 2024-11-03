using Moq;
using FluentAssertions;
using Persistence.Repositories;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Domain.Persistence.Configuration;
using Persistence.Database;
using Domain.Persistence.DTOs;
using Microsoft.Extensions.Options;
using SmartExcelAnalyzer.Tests.TestUtilities;

namespace SmartExcelAnalyzer.Tests.Persistence.Database;

public class VectorRepositoryAdditionalTests
{
    private readonly Mock<IDatabaseWrapper> _databaseMock = new();
    private readonly Mock<ILLMRepository> _llmRepositoryMock = new();
    private readonly Mock<ILogger<VectorRepository>> _loggerMock = new();
    private readonly Mock<IOptions<DatabaseOptions>> _databaseOptionsMock = new();
    private readonly Mock<IOptions<LLMServiceOptions>> _llmOptionsMock = new();
    private VectorRepository Sut => new(_databaseMock.Object, _loggerMock.Object, _llmRepositoryMock.Object, _llmOptionsMock.Object, _databaseOptionsMock.Object);

    public VectorRepositoryAdditionalTests()
    {
        _databaseOptionsMock.SetupGet(o => o.Value).Returns(new DatabaseOptions() { MAX_CONNECTION_COUNT = 10 });
        _llmOptionsMock.SetupGet(o => o.Value).Returns(new LLMServiceOptions() { COMPUTE_BATCH_SIZE = 100 });
    }

    [Fact]
    public async Task ComputeEmbeddingsAsync_ShouldLogWarning_WhenLLMReturnsNullEmbeddings()
    {
        var data = new SummarizedExcelData
        {
            Rows =
            [
                new() { ["col1"] = "val1" },
                new() { ["col2"] = "val2" }
            ]
        };
        _llmRepositoryMock.Setup(l => l.ComputeBatchEmbeddings(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[]?[] { null });

        await Sut.SaveDocumentAsync(data);
        _loggerMock.VerifyLog(LogLevel.Warning, "Embedding at index");
    }

    [Fact]
    public async Task SaveBatchOfRowEmbeddings_ShouldLogWarning_WhenStoreVectorsAsyncReturnsNull()
    {
        var data = new SummarizedExcelData
        {
            Rows =
            [
                new() { ["col1"] = "val1" },
                new() { ["col2"] = "val2" }
            ]
        };
        _llmRepositoryMock.Setup(l => l.ComputeBatchEmbeddings(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new float[] { 1.0f } });
        _databaseMock.Setup(c => c.StoreVectorsAsync(It.IsAny<ConcurrentBag<ConcurrentDictionary<string, object>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => null!);

        await Sut.SaveDocumentAsync(data);
        _loggerMock.VerifyLog(LogLevel.Warning, "Failed to save vectors to the database for document");
    }

    [Fact]
    public async Task SaveDocumentAsync_ShouldLogWarning_WhenSaveDocumentDataAsyncReturnsNull()
    {
        var data = new SummarizedExcelData
        {
            Rows = []
        };
        _databaseMock.Setup(c => c.StoreVectorsAsync(It.IsAny<ConcurrentBag<ConcurrentDictionary<string, object>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => null!);

        var result = await Sut.SaveDocumentAsync(data);

        result.Should().BeNullOrWhiteSpace();
        _loggerMock.VerifyLog(LogLevel.Warning, "Failed to save");
    }

    [Fact]
    public async Task SaveDocumentAsync_ShouldNotCallStoreSummaryAsync_WhenSummaryIsNull()
    {
        var data = new SummarizedExcelData
        {
            Rows =
            [
                new() { ["col1"] = "val1" }
            ],
            Summary = null
        };
        _llmRepositoryMock.Setup(l => l.ComputeBatchEmbeddings(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new float[] { 1.0f } });
        _databaseMock.Setup(c => c.StoreVectorsAsync(It.IsAny<ConcurrentBag<ConcurrentDictionary<string, object>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("1");

        await Sut.SaveDocumentAsync(data);

        _databaseMock.Verify(c => c.StoreSummaryAsync(It.IsAny<string>(), It.IsAny<ConcurrentDictionary<string, object>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
