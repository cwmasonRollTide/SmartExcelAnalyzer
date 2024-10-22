using Microsoft.AspNetCore.SignalR;

namespace Persistence.Hubs;

public interface IProgressHubWrapper
{
    public Task SendProgress(double parseProgress, double saveProgress);
}

public class ProgressHubWrapper(IHubContext<ProgressHub> hubContext) : IProgressHubWrapper
{
    public async Task SendProgress(double parseProgress, double saveProgress) => await hubContext.Clients.All.SendAsync("ReceiveProgress", parseProgress, saveProgress);
}

public class ProgressHub(IProgressHubWrapper progressHubWrapper) : Hub
{
    public async Task SendProgress(double parseProgress, double saveProgress) => await progressHubWrapper.SendProgress(parseProgress, saveProgress);
}
