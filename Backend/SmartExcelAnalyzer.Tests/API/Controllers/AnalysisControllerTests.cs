using API.Controllers;
using Application.Queries;
using Domain.Persistence.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SmartExcelAnalyzer.Tests.API.Controllers;

public class AnalysisControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly AnalysisController _controller;

    public AnalysisControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new AnalysisController(_mediatorMock.Object);
    }

    [Fact]
    public async Task SubmitQuery_ReturnsOkResult_WithQueryAnswer()
    {
        // Arrange
        var query = new SubmitQuery();
        var queryAnswer = new QueryAnswer();
        _mediatorMock.Setup(m => m.Send(query, default)).ReturnsAsync(queryAnswer);

        // Act
        var result = await _controller.SubmitQuery(query);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<QueryAnswer>(okResult.Value);
        Assert.Equal(queryAnswer, returnValue);
    }

    [Fact]
    public async Task UploadFile_ReturnsOkResult_WithDocumentId()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var documentId = "12345";
        _mediatorMock.Setup(m => m.Send(It.IsAny<UploadFileCommand>(), default)).ReturnsAsync(documentId);

        // Act
        var result = await _controller.UploadFile(fileMock.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<string>(okResult.Value);
        Assert.Equal(documentId, returnValue);
    }
}