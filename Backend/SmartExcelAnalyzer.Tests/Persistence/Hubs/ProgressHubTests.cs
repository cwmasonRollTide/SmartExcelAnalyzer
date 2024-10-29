using Moq;
using Persistence.Hubs;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;

namespace SmartExcelAnalyzer.Tests.Persistence.Hubs;

public class ProgressHubTests
{
    private readonly Mock<IClientProxy> _clientProxyMock = new();
    private readonly Mock<ILogger<ProgressHub>> _loggerMock = new();
    private readonly Mock<IHubContext<ProgressHub>> _hubContextMock = new();
    private ProgressHub Sut => new(_loggerMock.Object, _hubContextMock.Object);

    public ProgressHubTests()
    {
        _hubContextMock.Setup(h => h.Clients.All).Returns(_clientProxyMock.Object);
    }

    [Fact]
    public async Task SendProgress_Sends_Progress_To_All_Clients()
    {
        double progress = 50;
        double total = 100;

        await Sut.SendProgress(progress, total);

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

        await Sut.SendProgress(progress, total);

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

        await Sut.SendProgress(progress, total);

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