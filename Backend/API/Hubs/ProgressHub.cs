using Microsoft.AspNetCore.SignalR;

namespace API.Hubs;

public class ProgressHub : Hub
{
    public async Task UpdateProgress(string userId, double parseProgress, double saveProgress)
    {
        await Clients.User(userId).SendAsync("ReceiveProgress", parseProgress, saveProgress);
    }
}
