using Moq;
using Persistence.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace SmartExcelAnalyzer.Tests.Persistence.Hubs;

public class ProgressHubTests
{
    [Fact]
    public async Task SendProgress_ShouldSendProgressToAllClients()
    {
        var mockClients = new Mock<IHubCallerClients>();
        var mockClientProxy = new Mock<IClientProxy>();

        mockClients.Setup(clients => clients.All).Returns(mockClientProxy.Object);

        var hub = new ProgressHub
        {
            Clients = mockClients.Object
        };

        double parseProgress = 0.5;
        double saveProgress = 0.75;

        await hub.SendProgress(parseProgress, saveProgress);

        mockClientProxy.Verify(
            clientProxy => clientProxy.SendCoreAsync(
                "ReceiveProgress",
                It.Is<object[]>(objects => (double)objects[0] == parseProgress && (double)objects[1] == saveProgress),
                default
            ),
            Times.Once
        );
    }
}
