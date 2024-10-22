using Microsoft.AspNetCore.SignalR;

namespace Persistence.Hubs;

public interface IProgressHubWrapper
{
    public Task SendProgress(double parseProgress, double saveProgress);
}

public class ProgressHubWrapper(IHubContext<ProgressHub> hubContext) : IProgressHubWrapper
{
    private readonly IHubContext<ProgressHub> _hubContext = hubContext;

    public async Task SendProgress(double parseProgress, double saveProgress)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveProgress", parseProgress, saveProgress);
    }
}

public class ProgressHub(IProgressHubWrapper progressHubWrapper) : Hub
{
    private readonly IProgressHubWrapper _progressHubWrapper = progressHubWrapper;

    public async Task SendProgress(double parseProgress, double saveProgress)
    {
        await _progressHubWrapper.SendProgress(parseProgress, saveProgress);
    }
}
