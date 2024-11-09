using Moq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Persistence.Hubs;

public class ProgressHubTests
{
    private readonly Mock<IHubCallerClients> _mockClients;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly Mock<ILogger<ProgressHub>> _mockLogger;
    private readonly ProgressHub _hub;

    public ProgressHubTests()
    {
        _mockClients = new Mock<IHubCallerClients>();
        _mockClientProxy = new Mock<IClientProxy>();
        _mockLogger = new Mock<ILogger<ProgressHub>>();
        _hub = new ProgressHub(_mockLogger.Object);
        
        _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);
        _hub.Clients = _mockClients.Object;
    }

    [Fact]
    public async Task SendProgress_ShouldSendProgressToAllClients()
    {
        double progress = 50;
        double total = 100;
        var cancellationToken = CancellationToken.None;

        _mockClientProxy
            .Setup(x => x.SendCoreAsync(
                "ReceiveProgress",
                It.Is<object[]>(o => o[0].Equals(progress) && o[1].Equals(total)),
                cancellationToken))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await _hub.SendProgress(progress, total, cancellationToken);

        _mockClientProxy.Verify();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once);
    }

    [Fact]
    public async Task SendError_ShouldSendErrorToAllClients()
    {
        string errorMessage = "Test error message";

        _mockClientProxy
            .Setup(x => x.SendCoreAsync(
                "ReceiveError",
                It.Is<object[]>(o => o[0].Equals(errorMessage)),
                default))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await _hub.SendError(errorMessage);

        _mockClientProxy.Verify();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once);
    }
}
