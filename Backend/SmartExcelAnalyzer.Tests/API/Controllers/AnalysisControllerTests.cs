using Moq;
using MediatR;
using System.Text;
using API.Controllers;
using FluentAssertions;
using Application.Queries;
using Application.Commands;
using Domain.Persistence.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using API.Hubs;

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
    public async Task SubmitQuery_ReturnsBadRequest_WhenQueryIsInvalid()
    {
        var query = new SubmitQuery { Query = "test query", DocumentId = "doc1" };
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SubmitQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("test exception"));

        await Assert.ThrowsAsync<ArgumentException>(async () => await Sut.SubmitQuery(query));
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
    public async Task UploadFile_ReturnsBadRequest_WhenFileIsInvalid()
    {
        var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("test file")), 0, 0, "file", "test.txt");
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UploadFileCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("test exception"));

        await Assert.ThrowsAsync<ArgumentException>(async () => await Sut.UploadFile(file));
    }

    [Fact]
    public async Task UploadFile_ReportsProgress_DuringFileUpload()
    {
        var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("test file")), 0, 0, "file", "test.txt");
        var expectedResult = "doc1";
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UploadFileCommand>(), It.IsAny<CancellationToken>()))
            .Callback<UploadFileCommand, CancellationToken>((command, token) =>
            {
                var progress = (IProgress<(double, double)>)command.Progress;
                progress.Report((0.5, 0.5)); // Simulate 50% progress
            })
            .ReturnsAsync(expectedResult);

        var clientProxy = new Mock<IClientProxy>();
        _hubContextMock.Setup(h => h.Clients.All).Returns(clientProxy.Object);

        var result = await Sut.UploadFile(file);

        var okResult = Assert.IsType<OkObjectResult>(result);
        okResult.Value.Should().BeEquivalentTo(expectedResult);

        clientProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveProgress",
                It.Is<object[]>(o => o != null && o.Length == 2 && (double)o[0] == 0.5 && (double)o[1] == 0.5),
                default),
            Times.Once);
    }
}
