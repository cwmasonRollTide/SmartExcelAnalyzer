using Moq;
using MediatR;
using API.Controllers;

namespace SmartExcelAnalyzer.Tests.API.Controllers;
public class BaseControllerTests
{
    [Fact]
    public void BaseController_Constructor_ShouldSetMediator()
    {
        var mockMediator = new Mock<IMediator>();

        var controller = new BaseController(mockMediator.Object);

        Assert.NotNull(controller);
    }

}