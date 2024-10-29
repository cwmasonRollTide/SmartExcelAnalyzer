using Moq;
using Persistence.Hubs;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;

namespace SmartExcelAnalyzer.Tests.Persistence.Hubs;

public class ProgressHubTests
{
    private const double Total = 100;
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

        await Sut.SendProgress(progress, Total);

        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "ReceiveProgress",
                It.Is<object[]>(args => 
                    (double)args[0] == progress && 
                    (double)args[1] == Total),
                default
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SendProgress_WithZeroProgress_Sends_Progress_To_All_Clients()
    {
        double progress = 0;

        await Sut.SendProgress(progress, Total);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString() == $"Progress update: {progress}/{Total}"),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((o, t) => true)
            ),
            Times.Once
        );
        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "ReceiveProgress",
                It.Is<object[]>(args => 
                    (double)args[0] == progress && 
                    (double)args[1] == Total),
                default
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SendProgress_WithFullProgress_Sends_Progress_To_All_Clients()
    {
        double progress = 100;

        await Sut.SendProgress(progress, Total);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString() == $"Progress update: {progress}/{Total}"),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((o, t) => true)
            ),
            Times.Once
        );
        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "ReceiveProgress",
                It.Is<object[]>(args => 
                    (double)args[0] == progress && 
                    (double)args[1] == Total),
                default
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SendError_Sends_Error_To_All_Clients()
    {
        string message = "Error message";

        await Sut.SendError(message);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString() == $"Progress error: {message}"),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((o, t) => true)
            ),
            Times.Once
        );
        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "ReceiveError",
                It.Is<object[]>(args => 
                    (string)args[0] == message),
                default
            ),
            Times.Once
        );
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(25d)]
    [InlineData(27.275d)]
    [InlineData(99.999d)]
    [InlineData(100)]
    public async Task SendProgress_Sends_Progress_To_All_Clients_Theory_Version(double progress)
    {

        await Sut.SendProgress(progress, Total);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString() == $"Progress update: {progress}/{Total}"),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((o, t) => true)
            ),
            Times.Once
        );
        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "ReceiveProgress",
                It.Is<object[]>(args => 
                    (double)args[0] == progress && 
                    (double)args[1] == Total),
                default
            ),
            Times.Once
        );
    }
}