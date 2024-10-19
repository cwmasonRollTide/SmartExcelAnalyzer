using Moq;
using Application.Commands;
using Application.Services;
using Domain.Persistence.DTOs;
using Persistence.Repositories;
using Microsoft.AspNetCore.Http;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Logging;

namespace SmartExcelAnalyzer.Tests.Application;

public class UploadFileCommandTests
{
    public class UploadFileCommandValidatorTests
    {
        private readonly UploadFileCommandValidator _validator = new();
        [Fact]
        public void Validate_WhenFileIsNull_ShouldHaveValidationError()
        {
            var command = new UploadFileCommand { File = null! };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.File)
                .WithErrorMessage("File is required.");
        }

        [Fact]
        public void Validate_WhenFileIsNotNullButEmpty_ShouldHaveValidationError()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(0);
            var command = new UploadFileCommand { File = fileMock.Object };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.File)
                .WithErrorMessage("File is empty.");
        }

        [Fact]
        public void Validate_WhenFileIsValid_ShouldNotHaveValidationError()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1024); // 1KB file
            var command = new UploadFileCommand { File = fileMock.Object };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
    public class UploadFileCommandHandlerTests
    {
        private readonly UploadFileCommandHandler _handler;
        private readonly Mock<IExcelFileService> _excelServiceMock;
        private readonly Mock<IVectorDbRepository> _vectorDbRepositoryMock;
        private readonly Mock<ILogger<UploadFileCommandHandler>> _loggerMock;

        public UploadFileCommandHandlerTests()
        {
            _excelServiceMock = new Mock<IExcelFileService>();
            _vectorDbRepositoryMock = new Mock<IVectorDbRepository>();
            _loggerMock = new Mock<ILogger<UploadFileCommandHandler>>();
            _handler = new UploadFileCommandHandler(
                _excelServiceMock.Object,
                _vectorDbRepositoryMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_WhenExcelServiceReturnsNull_ShouldReturnNull()
        {
            var command = new UploadFileCommand { File = Mock.Of<IFormFile>() };
            _excelServiceMock.Setup(x => x.PrepareExcelFileForLLMAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SummarizedExcelData)null!);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Null(result);
            _vectorDbRepositoryMock.Verify(x => x.SaveDocumentAsync(It.IsAny<SummarizedExcelData>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenVectorDbRepositoryReturnsNull_ShouldReturnNull()
        {
            var command = new UploadFileCommand { File = Mock.Of<IFormFile>() };
            var summarizedData = new SummarizedExcelData();
            _excelServiceMock.Setup(x => x.PrepareExcelFileForLLMAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(summarizedData);
            _vectorDbRepositoryMock.Setup(x => x.SaveDocumentAsync(It.IsAny<SummarizedExcelData>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string)null!);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_WhenSuccessful_ShouldReturnDocumentId()
        {
            var command = new UploadFileCommand { File = Mock.Of<IFormFile>() };
            var summarizedData = new SummarizedExcelData();
            var expectedDocumentId = "test-document-id";
            _excelServiceMock.Setup(x => x.PrepareExcelFileForLLMAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(summarizedData);
            _vectorDbRepositoryMock.Setup(x => x.SaveDocumentAsync(It.IsAny<SummarizedExcelData>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDocumentId);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(expectedDocumentId, result);
        }

        [Fact]
        public async Task Handle_ShouldLogInformation()
        {
            var command = new UploadFileCommand { File = Mock.Of<IFormFile>() };
            var summarizedData = new SummarizedExcelData();
            var expectedDocumentId = "test-document-id";
            _excelServiceMock.Setup(x => x.PrepareExcelFileForLLMAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(summarizedData);
            _vectorDbRepositoryMock.Setup(x => x.SaveDocumentAsync(It.IsAny<SummarizedExcelData>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDocumentId);

            await _handler.Handle(command, CancellationToken.None);

            _loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)));
        }
    }
}