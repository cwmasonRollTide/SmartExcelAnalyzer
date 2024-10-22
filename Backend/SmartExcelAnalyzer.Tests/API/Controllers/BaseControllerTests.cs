using Moq;
using MediatR;
using API.Controllers;
using FluentAssertions;

namespace SmartExcelAnalyzer.Tests.API.Controllers;

public class BaseControllerTests
{
    [Fact]
    public void BaseController_Constructor_ShouldSetMediator()
    {
        var mockMediator = new Mock<IMediator>();

        var controller = new BaseController(mockMediator.Object);

        controller.Should().NotBeNull();
    }
}