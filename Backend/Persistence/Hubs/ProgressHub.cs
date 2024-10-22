using Microsoft.AspNetCore.SignalR;

namespace Persistence.Hubs;

public class ProgressHub(IProgressHubWrapper progressHubWrapper) : Hub
{
    private readonly IProgressHubWrapper _progressHubWrapper = progressHubWrapper;

    public async Task SendProgress(double parseProgress, double saveProgress)
    {
        await _progressHubWrapper.SendProgress(parseProgress, saveProgress);
    }
}
