using Moq;
using Application.Commands;
using Application.Services;
using Domain.Persistence.DTOs;
using Persistence.Repositories;
using Microsoft.AspNetCore.Http;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using API.Hubs;

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
        private readonly Mock<IHubContext<ProgressHub>> _hubContextMock;
        private readonly Mock<IClientProxy> _clientProxyMock;

        public UploadFileCommandHandlerTests()
        {
            _excelServiceMock = new Mock<IExcelFileService>();
            _vectorDbRepositoryMock = new Mock<IVectorDbRepository>();
            _loggerMock = new Mock<ILogger<UploadFileCommandHandler>>();
            _hubContextMock = new Mock<IHubContext<ProgressHub>>();
            _clientProxyMock = new Mock<IClientProxy>();
            
            _hubContextMock.Setup(h => h.Clients.All).Returns(_clientProxyMock.Object);
            
            _handler = new UploadFileCommandHandler(
                _excelServiceMock.Object,
                _vectorDbRepositoryMock.Object,
                _loggerMock.Object,
                _hubContextMock.Object);
        }

        [Fact]
        public async Task Handle_WhenExcelServiceReturnsNull_ShouldReturnNull()
        {
            var command = new UploadFileCommand { File = Mock.Of<IFormFile>() };
            _excelServiceMock.Setup(x => x.PrepareExcelFileForLLMAsync(It.IsAny<IFormFile>(), It.IsAny<IProgress<(double, double)>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SummarizedExcelData)null!);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().BeNull();
            _vectorDbRepositoryMock.Verify(x => x.SaveDocumentAsync(It.IsAny<SummarizedExcelData>(), It.IsAny<IProgress<(double, double)>?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenVectorDbRepositoryReturnsNull_ShouldReturnNull()
        {
            var command = new UploadFileCommand { File = Mock.Of<IFormFile>() };
            var summarizedData = new SummarizedExcelData();
            _excelServiceMock.Setup(x => x.PrepareExcelFileForLLMAsync(It.IsAny<IFormFile>(), It.IsAny<IProgress<(double, double)>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(summarizedData);
            _vectorDbRepositoryMock.Setup(x => x.SaveDocumentAsync(It.IsAny<SummarizedExcelData>(), It.IsAny<IProgress<(double, double)>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string)null!);

            var result = await _handler.Handle(command, CancellationToken.None);
            result.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WhenSuccessful_ShouldReturnDocumentId()
        {
            var command = new UploadFileCommand { File = Mock.Of<IFormFile>() };
            var summarizedData = new SummarizedExcelData();
            var expectedDocumentId = "test-document-id";
            _excelServiceMock.Setup(x => x.PrepareExcelFileForLLMAsync(It.IsAny<IFormFile>(), It.IsAny<IProgress<(double, double)>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(summarizedData);
            _vectorDbRepositoryMock.Setup(x => x.SaveDocumentAsync(It.IsAny<SummarizedExcelData>(), It.IsAny<IProgress<(double, double)>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDocumentId);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.Should().Be(expectedDocumentId);
        }

        [Fact]
        public async Task Handle_ShouldReportProgress()
        {
            var command = new UploadFileCommand { File = Mock.Of<IFormFile>() };
            var summarizedData = new SummarizedExcelData();
            var expectedDocumentId = "test-document-id";
            var progressReported = false;

            _excelServiceMock.Setup(x => x.PrepareExcelFileForLLMAsync(It.IsAny<IFormFile>(), It.IsAny<IProgress<(double, double)>?>(), It.IsAny<CancellationToken>()))
                .Callback<IFormFile, IProgress<(double, double)>?, CancellationToken>((_, progress, _) =>
                {
                    if (progress != null)
                    {
                        progress.Report((0.5, 0.5));
                        progressReported = true;
                    }
                })
                .ReturnsAsync(summarizedData);

            _vectorDbRepositoryMock.Setup(x => x.SaveDocumentAsync(It.IsAny<SummarizedExcelData>(), It.IsAny<IProgress<(double, double)>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDocumentId);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().Be(expectedDocumentId);
            progressReported.Should().BeTrue();
            _clientProxyMock.Verify(
                x => x.SendCoreAsync(
                    "ReceiveProgress",
                    It.Is<object[]>(o => o != null && o.Length == 2 && (double)o[0] == 0.5 && (double)o[1] == 0.5),
                    default),
                Times.Once);
        }
    }
}