using Domain.Persistence.Configuration;
using Domain.Persistence.DTOs;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Persistence.Repositories;
using Persistence.Repositories.API;

namespace SmartExcelAnalyzer.Tests.Persistence;

public class LLMRepositoryTests
{
    private readonly Mock<IOptions<LLMServiceOptions>> _optionsMock = new();
    private readonly Mock<ILLMServiceLoadBalancer> _loadBalancerMock = new();
    private readonly Mock<IWebRepository<float[]?>> _computeServiceMock = new();
    private readonly Mock<IWebRepository<QueryAnswer>> _queryServiceMock = new();
    private readonly Mock<IWebRepository<IEnumerable<float[]?>>> _batchComputeServiceMock = new();
    private LLMRepository Sut => new(_loadBalancerMock.Object, _computeServiceMock.Object, _batchComputeServiceMock.Object, _queryServiceMock.Object);
    private const int COMPUTE_BATCH_SIZE = 10;

    public LLMRepositoryTests()
    {
        _optionsMock.Setup(o => o.Value).Returns(new LLMServiceOptions { LLM_SERVICE_URL = "http://test.com", COMPUTE_BATCH_SIZE = COMPUTE_BATCH_SIZE });
    }

    [Fact]
    public async Task QueryLLM_ShouldCallQueryServiceWithCorrectParameters()
    {
        var documentId = "testDoc";
        var question = "testQuestion";
        var expectedAnswer = new QueryAnswer { Answer = "Test answer" };
        _queryServiceMock.Setup(q => q.PostAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAnswer);

        var result = await Sut.QueryLLM(documentId, question);

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

        var result = await Sut.ComputeEmbedding(text);

        result.Should().BeEquivalentTo(expectedEmbedding);
        _computeServiceMock.Verify(c => c.PostAsync(
            "http://test.com/compute_embedding",
            It.Is<object>(o => o.GetType().GetProperty("text").GetValue(o).ToString() == text),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

