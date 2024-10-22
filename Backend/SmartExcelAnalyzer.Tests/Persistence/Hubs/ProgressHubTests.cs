using Moq;
using Persistence.Hubs;

namespace SmartExcelAnalyzer.Tests.Persistence.Hubs;

public class ProgressHubTestsTest
{
    private readonly Mock<IProgressHubWrapper> _hubContextMock = new();
    private ProgressHub Sut => new(_hubContextMock.Object);

    public ProgressHubTestsTest()
    {
        _hubContextMock
            .Setup(h => h.SendProgress(It.IsAny<double>(), It.IsAny<double>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task SendProgressUpdate_ShouldInvokeClientMethod()
    {
        var progress = 50;

        await Sut.SendProgress(progress, progress);

        _hubContextMock.Verify(h => h.SendProgress(progress, progress), Times.Once);
    }
}