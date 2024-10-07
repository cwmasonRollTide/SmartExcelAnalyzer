using Moq;
using MediatR;
using API.Controllers;
using FluentAssertions;
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
    public async Task SubmitQuery_ReturnsOkResult_WithQueryAnswer()
    {
        var query = new SubmitQuery
        {
            Query = "Sample query",
            DocumentId = "SampleDocumentId"
        };
        var queryAnswer = new QueryAnswer
        {
            Answer = "Sample answer"
        };
        _mediatorMock
            .Setup(m => m.Send(query, default))
            .ReturnsAsync(queryAnswer);

        var result = await Sut.SubmitQuery(query);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<QueryAnswer>(okResult.Value);
        returnValue.Should().BeEquivalentTo(queryAnswer);
    }

    [Fact]
    public async Task UploadFile_ReturnsOkResult_WithDocumentId()
    {
        var fileMock = new Mock<IFormFile>();
        var documentId = "12345";
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UploadFileCommand>(), default))
            .ReturnsAsync(documentId);

        var result = await Sut.UploadFile(fileMock.Object);

        var okResult = Assert.IsType<OkObjectResult>(result);
        string returnValue = Assert.IsType<string>(okResult.Value);
        returnValue.Should().Be(documentId);
    }
}