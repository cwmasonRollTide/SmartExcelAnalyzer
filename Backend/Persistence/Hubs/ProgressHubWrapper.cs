using Microsoft.AspNetCore.SignalR;

namespace Persistence.Hubs;

public class ProgressHubWrapper(IHubContext<ProgressHub> hubContext) : IProgressHubWrapper
{
    private readonly IHubContext<ProgressHub> _hubContext = hubContext;

    public async Task SendProgress(double parseProgress, double saveProgress)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveProgress", parseProgress, saveProgress);
    }
}
