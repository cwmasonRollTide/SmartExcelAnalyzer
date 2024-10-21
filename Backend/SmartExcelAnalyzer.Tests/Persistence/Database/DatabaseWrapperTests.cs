using Moq;
using FluentAssertions;
using Persistence.Database;
using System.Collections.Concurrent;

namespace SmartExcelAnalyzer.Tests.Persistence.Database;

public class DatabaseWrapperTests
{
    private Mock<IDatabaseWrapper> Sut = new();

    [Fact]
    public async Task GetSummaryAsync_ReturnsExpectedSummary()
    {
        var expectedSummary = new ConcurrentDictionary<string, object>();
        expectedSummary["key"] = "value";
        Sut
            .Setup(db => db.GetSummaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSummary);

        var result = await Sut.Object.GetSummaryAsync("testDocumentId");

        result.Should().BeEquivalentTo(expectedSummary);
        result.Should().ContainKey("key").WhoseValue.Should().Be("value");
    }

    [Fact]
    public async Task StoreSummaryAsync_ReturnsExpectedValue()
    {
        var summary = new ConcurrentDictionary<string, object>();
        summary["key"] = "value";
        Sut
            .Setup(db => db.StoreSummaryAsync(It.IsAny<string>(), It.IsAny<ConcurrentDictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await Sut.Object.StoreSummaryAsync("testDocumentId", summary);

        result.Should().Be(1);
    }

    [Fact]
    public async Task StoreVectorsAsync_ReturnsExpectedDocumentId()
    {
        var rows = new List<ConcurrentDictionary<string, object>>();
        var expectedDocId = "testDocId";
        Sut
            .Setup(db => db.StoreVectorsAsync(It.IsAny<IEnumerable<ConcurrentDictionary<string, object>>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDocId);

        var result = await Sut.Object.StoreVectorsAsync(rows);

        result.Should().Be(expectedDocId);
    }

    [Fact]
    public async Task GetRelevantDocumentsAsync_ReturnsExpectedDocuments()
    {
        var expectedDocuments = new List<ConcurrentDictionary<string, object>>
        {
            new() { ["key1"] = "value1" },
            new() { ["key2"] = "value2" }
        };
        Sut
            .Setup(db => db.GetRelevantDocumentsAsync(It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDocuments);

        var result = await Sut.Object.GetRelevantDocumentsAsync("testDocumentId", [1.0f, 2.0f], 2);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDocuments);
        result.Should().HaveCount(2);
        result.ElementAt(0).Should().ContainKey("key1").WhoseValue.Should().Be("value1");
    }
}
