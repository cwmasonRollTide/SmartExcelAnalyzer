using Domain.Persistence.Configuration;
using Domain.Persistence.DTOs;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Persistence.Repositories;

namespace SmartExcelAnalyzer.Tests.Persistence;
public class LLMRepositoryTests
{
    private readonly Mock<IOptions<LLMServiceOptions>> _optionsMock;
    private readonly Mock<IWebRepository<float[]?>> _computeServiceMock;
    private readonly Mock<IWebRepository<QueryAnswer>> _queryServiceMock;
    private readonly LLMRepository _repository;

    public LLMRepositoryTests()
    {
        _optionsMock = new Mock<IOptions<LLMServiceOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(new LLMServiceOptions { LLM_SERVICE_URL = "http://test.com" });
        _computeServiceMock = new Mock<IWebRepository<float[]?>>();
        _queryServiceMock = new Mock<IWebRepository<QueryAnswer>>();
        _repository = new LLMRepository(_optionsMock.Object, _computeServiceMock.Object, _queryServiceMock.Object);
    }

    [Fact]
    public async Task QueryLLM_ShouldCallQueryServiceWithCorrectParameters()
    {
        var documentId = "testDoc";
        var question = "testQuestion";
        var expectedAnswer = new QueryAnswer { Answer = "Test answer" };
        _queryServiceMock.Setup(q => q.PostAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAnswer);

        var result = await _repository.QueryLLM(documentId, question);

        result.Should().BeEquivalentTo(expectedAnswer);
        _queryServiceMock.Verify(q => q.PostAsync(
            "http://test.com/query",
            It.Is<object>(o => 
                o.GetType().GetProperty("document_id").GetValue(o).ToString() == documentId &&
                o.GetType().GetProperty("question").GetValue(o).ToString() == question
            ),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ComputeEmbedding_ShouldCallComputeServiceWithCorrectParameters()
    {
        var text = "test text";
        var expectedEmbedding = new float[] { 1.0f, 2.0f, 3.0f };
        _computeServiceMock.Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmbedding);

        var result = await _repository.ComputeEmbedding(text);

        result.Should().BeEquivalentTo(expectedEmbedding);
        _computeServiceMock.Verify(c => c.PostAsync(
            "http://test.com/compute_embedding",
            It.Is<object>(o => o.GetType().GetProperty("text").GetValue(o).ToString() == text),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

