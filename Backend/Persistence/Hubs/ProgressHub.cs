using Microsoft.AspNetCore.SignalR;

namespace Persistence.Hubs;

public class ProgressHub : Hub
{
    public async Task SendProgress(double parseProgress, double saveProgress) => await Clients.All.SendAsync("ReceiveProgress", parseProgress, saveProgress);
}