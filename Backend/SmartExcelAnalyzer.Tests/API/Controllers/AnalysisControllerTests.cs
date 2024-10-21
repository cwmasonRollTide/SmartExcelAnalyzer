using Moq;
using MediatR;
using API.Controllers;
using Application.Queries;
using Domain.Persistence.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text;
using Application.Commands;

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
    public async Task SubmitQuery_ReturnsBadRequest_WhenQueryIsInvalid()
    {
        var query = new SubmitQuery { Query = "test query", DocumentId = "doc1" };
        _mediatorMock.Setup(m => m.Send(It.IsAny<SubmitQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("test exception"));

        await Assert.ThrowsAsync<ArgumentException>(async () => await Sut.SubmitQuery(query));
    }

    [Fact]
    public async Task UploadFile_ReturnsOkResult_WhenFileIsValid()
    {
        var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("test file")), 0, 0, "file", "test.txt");
        var expectedResult = "doc1";
        _mediatorMock.Setup(m => m.Send(It.IsAny<UploadFileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await Sut.UploadFile(file);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task UploadFile_ReturnsBadRequest_WhenFileIsInvalid()
    {
        var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("test file")), 0, 0, "file", "test.txt");
        _mediatorMock.Setup(m => m.Send(It.IsAny<UploadFileCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("test exception"));

        await Assert.ThrowsAsync<ArgumentException>(async () => await Sut.UploadFile(file));
    }
}
