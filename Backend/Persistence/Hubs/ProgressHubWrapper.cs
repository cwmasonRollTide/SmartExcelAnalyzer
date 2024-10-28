using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Persistence.Hubs;

public class ProgressHubWrapper(
    IHubContext<ProgressHub> hubContext, 
    ILogger<IProgressHubWrapper> _logger
) : IProgressHubWrapper
{
    private readonly IHubContext<ProgressHub> _hubContext = hubContext;

    public async Task SendProgress(double progress, double total) 
    {
        _logger.LogInformation("Progress update: {Progress}/{Total}", progress, total);
        if (progress == total) _logger.LogInformation("Progress complete: {Message}", "Progress complete");
        await SendCompletion("Progress complete");
    }

    public async Task SendCompletion(string message) =>
        await _hubContext.Clients.All.SendAsync("ReceiveCompletion", message);

    public async Task SendError(string message) =>
        await _hubContext.Clients.All.SendAsync("ReceiveError", message);
}
