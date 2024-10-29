using Moq;
using Persistence.Hubs;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;

namespace SmartExcelAnalyzer.Tests.Persistence.Hubs;

public class ProgressHubTests
{
    private readonly Mock<ILogger<ProgressHub>> _loggerMock;
    private readonly Mock<IHubContext<ProgressHub>> _hubContextMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly ProgressHub _progressHub;

    public ProgressHubTests()
    {
        _loggerMock = new Mock<ILogger<ProgressHub>>();
        _hubContextMock = new Mock<IHubContext<ProgressHub>>();
        _clientProxyMock = new Mock<IClientProxy>();

        _hubContextMock.Setup(h => h.Clients.All).Returns(_clientProxyMock.Object);

        _progressHub = new ProgressHub(_loggerMock.Object, _hubContextMock.Object);
    }

    [Fact]
    public async Task SendProgress_Sends_Progress_To_All_Clients()
    {
        double progress = 50;
        double total = 100;

        await _progressHub.SendProgress(progress, total);

        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "ReceiveProgress",
                It.Is<object[]>(args => 
                    (double)args[0] == progress && 
                    (double)args[1] == total),
                default
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SendProgress_WithZeroProgress_Sends_Progress_To_All_Clients()
    {
        double progress = 0;
        double total = 100;

        await _progressHub.SendProgress(progress, total);

        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "ReceiveProgress",
                It.Is<object[]>(args => 
                    (double)args[0] == progress && 
                    (double)args[1] == total),
                default
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SendProgress_WithFullProgress_Sends_Progress_To_All_Clients()
    {
        double progress = 100;
        double total = 100;

        await _progressHub.SendProgress(progress, total);

        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "ReceiveProgress",
                It.Is<object[]>(args => 
                    (double)args[0] == progress && 
                    (double)args[1] == total),
                default
            ),
            Times.Once
        );
    }
}