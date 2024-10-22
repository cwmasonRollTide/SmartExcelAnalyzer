using Microsoft.AspNetCore.SignalR;
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

public class ProgressHubWrapperTests
{
    private readonly Mock<IHubContext<ProgressHub>> _hubContextMock = new();
    private readonly Mock<IClientProxy> _clientProxyMock = new();
    private ProgressHubWrapper Sut => new(_hubContextMock.Object);

    public ProgressHubWrapperTests()
    {
        _clientProxyMock
            .Setup(x => x.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _hubContextMock
            .Setup(h => h.Clients.All)
            .Returns(_clientProxyMock.Object);
    }

    [Fact]
    public async Task SendProgressUpdate_ShouldInvokeClientMethod()
    {
        var progress = 50;

        await Sut.SendProgress(progress, progress);

        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "ReceiveProgress",
                It.Is<object[]>(args => 
                    args.Length == 2 && 
                    (double)args[0] == progress && 
                    (double)args[1] == progress),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}