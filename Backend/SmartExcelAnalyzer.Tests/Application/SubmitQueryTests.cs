using Moq;
using Application.Queries;
using Domain.Persistence.DTOs;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Logging;
using Persistence.Repositories;
using System.Collections.Concurrent;

namespace Application.Tests.Queries;
public class SubmitQueryTests
{
    public class SubmitQueryValidatorTests
    {
        private SubmitQueryValidator _validator = new();

        [Fact]
        public void Validate_WhenQueryIsNull_ShouldHaveValidationError()
        {
            var query = new SubmitQuery { Query = null!, DocumentId = "doc1" };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.Query)
                .WithErrorMessage("Query is required.");
        }

        [Fact]
        public void Validate_WhenQueryIsEmpty_ShouldHaveValidationError()
        {
            var query = new SubmitQuery { Query = "", DocumentId = "doc1" };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.Query)
                .WithErrorMessage("Query is required.");
        }

        [Fact]
        public void Validate_WhenDocumentIdIsNull_ShouldHaveValidationError()
        {
            var query = new SubmitQuery { Query = "test", DocumentId = null! };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.DocumentId)
                .WithErrorMessage("DocumentId is required.");
        }

        [Fact]
        public void Validate_WhenDocumentIdIsEmpty_ShouldHaveValidationError()
        {
            var query = new SubmitQuery { Query = "test", DocumentId = "" };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.DocumentId)
                .WithErrorMessage("DocumentId is required.");
        }

        [Fact]
        public void Validate_WhenRelevantRowsCountIsNegative_ShouldHaveValidationError()
        {
            var query = new SubmitQuery { Query = "test", DocumentId = "doc1", RelevantRowsCount = -1 };
            var result = _validator.TestValidate(query);
            result.ShouldHaveValidationErrorFor(x => x.RelevantRowsCount)
                .WithErrorMessage("RelevantRowsCount must be greater than or equal to 0.");
        }

        [Fact]
        public void Validate_WhenAllFieldsAreValid_ShouldNotHaveValidationError()
        {
            var query = new SubmitQuery { Query = "test", DocumentId = "doc1", RelevantRowsCount = 5 };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WhenRelevantRowsCountIsNull_ShouldNotHaveValidationError()
        {
            var query = new SubmitQuery { Query = "test", DocumentId = "doc1", RelevantRowsCount = null };
            var result = _validator.TestValidate(query);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }

    public class SubmitQueryHandlerTests
    {
        private readonly SubmitQueryHandler _handler;
        private readonly Mock<ILLMRepository> _llmRepositoryMock;
        private readonly Mock<IVectorDbRepository> _vectorDbRepositoryMock;
        private readonly Mock<ILogger<SubmitQueryHandler>> _loggerMock;

        public SubmitQueryHandlerTests()
        {
            _llmRepositoryMock = new Mock<ILLMRepository>();
            _vectorDbRepositoryMock = new Mock<IVectorDbRepository>();
            _loggerMock = new Mock<ILogger<SubmitQueryHandler>>();
            _handler = new SubmitQueryHandler(
                _llmRepositoryMock.Object,
                _loggerMock.Object,
                _vectorDbRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_WhenLLMQueryFails_ShouldReturnNull()
        {
            var query = new SubmitQuery { Query = "test", DocumentId = "doc1" };
            _llmRepositoryMock.Setup(x => x.QueryLLM(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((QueryAnswer)null!);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().BeNull();
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to query LLM")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenLLMQuerySucceedsWithoutRelevantRows_ShouldReturnAnswer()
        {
            var query = new SubmitQuery { Query = "test", DocumentId = "doc1" };
            var expectedAnswer = new QueryAnswer { Answer = "Test answer" };
            _llmRepositoryMock.Setup(x => x.QueryLLM(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedAnswer);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().NotBeNull();
            result!.Answer.Should().Be(expectedAnswer.Answer);
            result.RelevantRows.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_WhenComputeEmbeddingFails_ShouldReturnNull()
        {
            var query = new SubmitQuery { Query = "test", DocumentId = "doc1", RelevantRowsCount = 5 };
            _llmRepositoryMock.Setup(x => x.QueryLLM(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryAnswer { Answer = "Test answer" });
            _llmRepositoryMock.Setup(x => x.ComputeEmbedding(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((float[]?)null);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().BeNull();
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to compute embedding")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenVectorDbQueryFails_ShouldReturnNull()
        {
            var query = new SubmitQuery { Query = "test", DocumentId = "doc1", RelevantRowsCount = 5 };
            _llmRepositoryMock.Setup(x => x.QueryLLM(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueryAnswer { Answer = "Test answer" });
            _llmRepositoryMock.Setup(x => x.ComputeEmbedding(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([1.0f, 2.0f, 3.0f]);
            _vectorDbRepositoryMock.Setup(x => x.QueryVectorData(It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SummarizedExcelData)null!);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().BeNull();
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to query VectorDb")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenEverythingSucceeds_ShouldReturnCompleteAnswer()
        {
            var query = new SubmitQuery { Query = "test", DocumentId = "doc1", RelevantRowsCount = 5 };
            var expectedAnswer = new QueryAnswer { Answer = "Test answer" };
            var row = new ConcurrentDictionary<string, object>();
            row.TryAdd("col1", "value1");
            var expectedRelevantRows = new ConcurrentBag<ConcurrentDictionary<string, object>> { row };
            _llmRepositoryMock.Setup(x => x.QueryLLM(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedAnswer);
            _llmRepositoryMock.Setup(x => x.ComputeEmbedding(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new float[] { 1.0f, 2.0f, 3.0f });
            _vectorDbRepositoryMock.Setup(x => x.QueryVectorData(It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SummarizedExcelData { Rows = expectedRelevantRows });

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().NotBeNull();
            result!.Answer.Should().Be(expectedAnswer.Answer);
            result.RelevantRows.Should().BeEquivalentTo(expectedRelevantRows);
        }
    }
}