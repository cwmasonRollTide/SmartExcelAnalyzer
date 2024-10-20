using Moq;
using MediatR;
using API.Controllers;
using Application.Queries;
using Application.Commands;
using Domain.Persistence.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace SmartExcelAnalyzer.Tests.API.Controllers;

public class AnalysisControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private AnalysisController Sut => new(_mediatorMock.Object);

    [Fact]
    public async Task SubmitQuery_ReturnsOkResult_WhenQueryIsValid()
    {
        var query = new SubmitQuery { Query = "test query", DocumentId = "doc1" };
        var expectedResult = new QueryAnswer { Answer = "test answer" };
        _mediatorMock.Setup(m => m.Send(It.IsAny<SubmitQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await Sut.SubmitQuery(query);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task SubmitQuery_ReturnsBadRequest_WhenResultIsNull()
    {
        var query = new SubmitQuery { Query = "invalid query", DocumentId = "doc1" };
        _mediatorMock.Setup(m => m.Send(It.IsAny<SubmitQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueryAnswer)null!);

        var result = await Sut.SubmitQuery(query);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid query or unable to process the request.", badRequestResult.Value);
    }

    [Fact]
    public async Task SubmitQuery_ReturnsInternalServerError_WhenExceptionOccurs()
    {
        var query = new SubmitQuery { Query = "test query", DocumentId = "doc1" };
        _mediatorMock.Setup(m => m.Send(It.IsAny<SubmitQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        var result = await Sut.SubmitQuery(query);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while processing your request.", statusCodeResult.Value);
    }

    [Fact]
    public async Task UploadFile_ReturnsBadRequest_WhenFileIsNull()
    {
        var result = await Sut.UploadFile(null!);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No file uploaded.", badRequestResult.Value);
    }

    [Fact]
    public async Task UploadFile_ReturnsBadRequest_WhenFileIsEmpty()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(0);

        var result = await Sut.UploadFile(fileMock.Object);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No file uploaded.", badRequestResult.Value);
    }

    [Fact]
    public async Task UploadFile_ReturnsBadRequest_WhenUploadFails()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1);
        _mediatorMock.Setup(m => m.Send(It.IsAny<UploadFileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null!);

        var result = await Sut.UploadFile(fileMock.Object);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("File upload failed.", badRequestResult.Value);
    }

    [Fact]
    public async Task UploadFile_ReturnsInternalServerError_WhenExceptionOccurs()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1);
        _mediatorMock.Setup(m => m.Send(It.IsAny<UploadFileCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        var result = await Sut.UploadFile(fileMock.Object);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while uploading the file.", statusCodeResult.Value);
    }

    
}
