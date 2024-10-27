using Microsoft.AspNetCore.SignalR;

namespace Persistence.Hubs;

public interface IProgressHubWrapper
{
    public Task SendProgress(double parseProgress, double saveProgress);
}

public class ProgressHubWrapper(
    IHubContext<ProgressHub> hubContext
) : IProgressHubWrapper
{
    private const string SignalRMethod = "ReceiveProgress";
    public async Task SendProgress(double parseProgress, double saveProgress) => 
        await hubContext
            .Clients
            .All
            .SendAsync(
                SignalRMethod, 
                parseProgress, 
                saveProgress
            );
}

public class ProgressHub(IProgressHubWrapper progressHubWrapper) : Hub
{
    public async Task SendProgress(double parseProgress, double saveProgress) => await progressHubWrapper.SendProgress(parseProgress, saveProgress);
}
