using System.Collections.Concurrent;
using Domain.Persistence;
using Domain.Persistence.DTOs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Persistence;
using Persistence.Repositories;
using SmartExcelAnalyzer.Tests.TestUtilities;

namespace SmartExcelAnalyzer.Tests.Persistence;

public class VectorDbRepositoryTests
{
    private readonly VectorDbRepository _repository;
    private readonly Mock<ILLMRepository> _llmRepositoryMock;
    private readonly Mock<ApplicationDbContext> _contextMock;
    private readonly Mock<ILogger<VectorDbRepository>> _loggerMock;
    private readonly MockDbSet<Document> _mockDocumentSet;
    private readonly MockDbSet<Summary> _mockSummarySet;

    public VectorDbRepositoryTests()
    {
        _llmRepositoryMock = new Mock<ILLMRepository>();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        _mockDocumentSet = new MockDbSet<Document>([]);
        _mockSummarySet = new MockDbSet<Summary>([]);
        _contextMock = new Mock<ApplicationDbContext>(options);

        _contextMock.Setup(c => c.Documents).Returns(_mockDocumentSet.Object);
        _contextMock.Setup(c => c.Summaries).Returns(_mockSummarySet.Object);
        _loggerMock = new Mock<ILogger<VectorDbRepository>>();
        _repository = new VectorDbRepository(_llmRepositoryMock.Object, _contextMock.Object, _loggerMock.Object);
    }

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
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _repository.SaveDocumentAsync(data);

        result.Should().NotBeNullOrEmpty();
        _contextMock.Verify(c => c.Documents.AddRange(It.IsAny<Document[]>()), Times.Once);
        _contextMock.Verify(c => c.Summaries.Add(It.IsAny<Summary>()), Times.Once);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
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
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var result = await _repository.SaveDocumentAsync(data);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveDocumentAsync_ShouldReturnNullWhenSavingSummaryFails()
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
        _contextMock.SetupSequence(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(-1)
            .ReturnsAsync(1);

        var result = await _repository.SaveDocumentAsync(data);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveDocumentAsync_ShouldReturnNullWhenSavingRowsSucceeds_ButSavingSummaryFails()
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
        _contextMock.SetupSequence(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(-1)
            .ReturnsAsync(4);

        var result = await _repository.SaveDocumentAsync(data);

        result.Should().BeNull();
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

        _mockDocumentSet.UpdateData(documents);
        _mockSummarySet.UpdateData(summaries);

        var result = await _repository.QueryVectorData(documentId, queryVector);

        result.Should().NotBeNull();
        result.Rows.Should().HaveCount(1);
        result.Rows!.First().Should().ContainKey("col1");
        result.Summary.Should().ContainKey("sum");
        result.Summary!["sum"].ToString().Should().BeEquivalentTo("10");
    }
}