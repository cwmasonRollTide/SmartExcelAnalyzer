using Xunit;
using Moq;
using System.Collections.Concurrent;
using Persistence.Database;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace SmartExcelAnalyzer.Tests.Persistence.Database;

public class DatabaseWrapperTests
{
    private readonly Mock<IDatabaseWrapper> _mockDatabaseWrapper;

    public DatabaseWrapperTests()
    {
        _mockDatabaseWrapper = new Mock<IDatabaseWrapper>();
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsExpectedSummary()
    {
        // Arrange
        var expectedSummary = new ConcurrentDictionary<string, object>();
        expectedSummary["key"] = "value";
        _mockDatabaseWrapper.Setup(db => db.GetSummaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _mockDatabaseWrapper.Object.GetSummaryAsync("testDocumentId");

        // Assert
        Assert.Equal(expectedSummary, result);
    }

    [Fact]
    public async Task StoreSummaryAsync_ReturnsExpectedValue()
    {
        // Arrange
        var summary = new ConcurrentDictionary<string, object>();
        summary["key"] = "value";
        _mockDatabaseWrapper.Setup(db => db.StoreSummaryAsync(It.IsAny<string>(), It.IsAny<ConcurrentDictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _mockDatabaseWrapper.Object.StoreSummaryAsync("testDocumentId", summary);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task StoreVectorsAsync_ReturnsExpectedDocumentId()
    {
        // Arrange
        var rows = new List<ConcurrentDictionary<string, object>>();
        var expectedDocId = "testDocId";
        _mockDatabaseWrapper.Setup(db => db.StoreVectorsAsync(It.IsAny<IEnumerable<ConcurrentDictionary<string, object>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDocId);

        // Act
        var result = await _mockDatabaseWrapper.Object.StoreVectorsAsync(rows);

        // Assert
        Assert.Equal(expectedDocId, result);
    }

    [Fact]
    public async Task GetRelevantDocumentsAsync_ReturnsExpectedDocuments()
    {
        // Arrange
        var expectedDocuments = new List<ConcurrentDictionary<string, object>>
        {
            new ConcurrentDictionary<string, object> { ["key1"] = "value1" },
            new ConcurrentDictionary<string, object> { ["key2"] = "value2" }
        };
        _mockDatabaseWrapper.Setup(db => db.GetRelevantDocumentsAsync(It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDocuments);

        // Act
        var result = await _mockDatabaseWrapper.Object.GetRelevantDocumentsAsync("testDocumentId", new float[] { 1.0f, 2.0f }, 2);

        // Assert
        Assert.Equal(expectedDocuments, result);
    }
}
