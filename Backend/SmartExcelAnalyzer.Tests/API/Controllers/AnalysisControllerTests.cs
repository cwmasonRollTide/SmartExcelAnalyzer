using Moq;
using MediatR;
using System.Text;
using API.Controllers;
using FluentAssertions;
using Persistence.Hubs;
using Application.Queries;
using Application.Commands;
using Domain.Persistence.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System.ComponentModel.DataAnnotations;

namespace SmartExcelAnalyzer.Tests.API.Controllers;

public class AnalysisControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IHubContext<ProgressHub>> _hubContextMock = new();
    private AnalysisController Sut => new(_mediatorMock.Object, _hubContextMock.Object);

    [Fact]
    public async Task SubmitQuery_ReturnsOkResult_WhenQueryIsValid()
    {
        var query = new SubmitQuery { Query = "test query", DocumentId = "doc1" };
        var expectedResult = new QueryAnswer { Answer = "test answer" };
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SubmitQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await Sut.SubmitQuery(query);

        var okResult = Assert.IsType<OkObjectResult>(result);
        okResult.Value.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task GetQuery_ReturnsBadArgumentResult_WhenQueryIsNull()
    {
        _mediatorMock.Setup(m => m.Send(It.Is<SubmitQuery>(q => q.Query == null), It.IsAny<CancellationToken>())).ThrowsAsync(new ValidationException("Query is required"));

        await Assert.ThrowsAsync<ValidationException>(async () => await Sut.SubmitQuery(query: new SubmitQuery { Query = null!, DocumentId = "doc1" }));
    }

    [Fact]
    public async Task GetQuery_ReturnsServerError_WhenErrorUncaught()
    {
        var testQuery = "test query";
        _mediatorMock.Setup(m => m.Send(It.Is<SubmitQuery>(y => y.Query == testQuery), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Test exception"));

        await Assert.ThrowsAsync<Exception>(async () => await Sut.SubmitQuery(query: new SubmitQuery { Query = testQuery, DocumentId = "doc1" }));
    }

    [Fact]
    public async Task UploadFile_ReturnsOkResult_WhenFileIsValid()
    {
        var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("test file")), 0, 0, "file", "test.txt");
        var expectedResult = "doc1";
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UploadFileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await Sut.UploadFile(file);

        var okResult = Assert.IsType<OkObjectResult>(result);
        okResult.Value.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task UploadCommand_ReturnsBadArgumentResult_WhenFileIsNull()
    {
       _mediatorMock.Setup(m => m.Send(It.Is<UploadFileCommand>(y => y.File == null), It.IsAny<CancellationToken>())).ThrowsAsync(new ValidationException("File is required"));

        await Assert.ThrowsAsync<ValidationException>(async () => await Sut.UploadFile(file: null!));
    }

    [Fact]
    public async Task UploadCommand_ReturnsServerError_WhenErrorUncaught()
    {
        var testFile = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("test file")), 0, 0, "file", "test.txt");
        _mediatorMock.Setup(m => m.Send(It.Is<UploadFileCommand>(y => y.File == testFile), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Test exception"));

        await Assert.ThrowsAsync<Exception>(async () => await Sut.UploadFile(file: testFile));
    }
}
