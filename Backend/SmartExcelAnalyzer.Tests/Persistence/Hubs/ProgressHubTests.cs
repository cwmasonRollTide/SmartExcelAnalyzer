using Moq;
using Persistence.Hubs;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using SmartExcelAnalyzer.Tests.TestUtilities;

namespace SmartExcelAnalyzer.Tests.Persistence.Hubs;

public class ProgressHubTests
{
    private const double Total = 100;
    private readonly Mock<IClientProxy> _clientProxyMock = new();
    private readonly Mock<ILogger<ProgressHub>> _loggerMock = new();
    private readonly Mock<IProgressHubWrapper> _hubWrapperMock = new();
    private ProgressHub Sut => new(_loggerMock.Object, _hubWrapperMock.Object);

    public ProgressHubTests()
    {
        _hubWrapperMock
            .Setup(x => x.SendProgress(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task SendProgress_Sends_Progress_To_All_Clients()
    {
        double progress = 50;

        await Sut.SendProgress(progress, Total);

        _loggerMock.VerifyLog(LogLevel.Information, $"Progress update: {progress}/{Total}");
        _hubWrapperMock.Verify(x => x.SendProgress(progress, Total, default), Times.Once);
    }

    [Fact]
    public async Task SendProgress_WithZeroProgress_Sends_Progress_To_All_Clients()
    {
        double progress = 0;

        await Sut.SendProgress(progress, Total);

        _loggerMock.VerifyLog(LogLevel.Information, $"Progress update: {progress}/{Total}");
        _hubWrapperMock.Verify(x => x.SendProgress(progress, Total, default), Times.Once);
    }

    [Fact]
    public async Task SendProgress_WithFullProgress_Sends_Progress_To_All_Clients()
    {
        double progress = 100;

        await Sut.SendProgress(progress, Total);

        _loggerMock.VerifyLog(LogLevel.Information, $"Progress update: {progress}/{Total}");
        _hubWrapperMock.Verify(x => x.SendProgress(progress, Total, default), Times.Once);
    }

    [Fact]
    public async Task SendError_Sends_Error_To_All_Clients()
    {
        string message = "Error message";

        await Sut.SendError(message);

        _loggerMock.VerifyLog(LogLevel.Error, message);
        _hubWrapperMock.Verify(x => x.SendError(message), Times.Once);
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

        _loggerMock.VerifyLog(LogLevel.Information, $"Progress update: {progress}/{Total}");
        _hubWrapperMock.Verify(x => x.SendProgress(progress, Total, default), Times.Once);
    }
}