using Moq;
using Persistence.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace SmartExcelAnalyzer.Tests.Persistence.Hubs;

public class ProgressHubWrapperTests
{
    private readonly Mock<IClientProxy> _clientProxyMock = new();
    private readonly Mock<ILogger<ProgressHub>> _loggerMock = new();
    private readonly Mock<IHubContext<ProgressHub>> _hubContextMock = new();
    private ProgressHub Sut => new(_loggerMock.Object, _hubContextMock.Object);

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
        var total = 100;

        await Sut.SendProgress(progress, total);

        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "ReceiveProgress",
                It.Is<object[]>(args =>
                    args.Length == 2 &&
                    (double)args[0] == progress &&
                    (double)args[1] == total),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}