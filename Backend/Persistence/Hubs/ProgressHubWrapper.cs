using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Persistence.Hubs;

public class ProgressHubWrapper(
    IHubContext<ProgressHub> _hubContext, 
    ILogger<IProgressHubWrapper> _logger
) : IProgressHubWrapper
{
    private const string ReceiveErrorMethod = "ReceiveError";
    private const string ReceiveProgressMethod = "ReceiveProgress";
    private const string ReceiveCompletionMethod = "ReceiveCompletion";
    private const string ProgressCompleteMessage = "Progress complete";
    private const string ProgressCompleteLogMessage = "Progress complete: {Message}";
    private const string ProgressUpdateMessage = "Progress update: {Progress}/{Total}";

    public async Task SendProgress(double progress, double total) 
    {
        _logger.LogInformation(ProgressUpdateMessage, progress, total);

        if (progress.Equals(total)) 
            _logger.LogInformation(ProgressCompleteLogMessage, ProgressCompleteMessage);

        await _hubContext
            .Clients
            .All
            .SendAsync(
                ReceiveProgressMethod, 
                progress, 
                total
            );
    }

    public async Task SendError(string message) => await _hubContext.Clients.All.SendAsync(ReceiveErrorMethod, message);

    public async Task SendCompletion(string message) => await _hubContext.Clients.All.SendAsync(ReceiveCompletionMethod, message);
}
