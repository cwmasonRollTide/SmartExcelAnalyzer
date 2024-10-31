using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Persistence.Hubs;

public class ProgressHub(
    ILogger<ProgressHub> logger, 
    IHubContext<ProgressHub> hubContext
) : Hub, IProgressHubWrapper
{
    #region SignalR methods
    private const string RECEIVE_ERROR = "ReceiveError";
    private const string RECEIVE_PROGRESS = "ReceiveProgress";
    #endregion

    #region Log message templates
    private const string PROGRESS_ERROR = "Progress error: {Message}";
    private const string CLIENT_CONNECTED = "Client connected: {ConnectionId}";
    private const string PROGRESS_UPDATE = "Progress update: {Progress}/{Total}";
    private const string CLIENT_DISCONNECTED = "Client disconnected: {ConnectionId}";
    #endregion

    #region SignalR client methods
    public async Task SendProgress(double progress, double total)
    {
        logger.LogInformation(PROGRESS_UPDATE, progress, total);
        await hubContext
            .Clients
            .All
            .SendAsync(
                RECEIVE_PROGRESS, 
                progress, 
                total
            );
    }

    public async Task SendError(string message)
    {
        logger.LogError(PROGRESS_ERROR, message);
        await hubContext
            .Clients
            .All
            .SendAsync(
                RECEIVE_ERROR, 
                message
            );
    }
    #endregion

    #region SignalR Connection methods
    [ExcludeFromCodeCoverage]
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation(CLIENT_CONNECTED, Context.ConnectionId);
        await base.OnConnectedAsync();
    }
    [ExcludeFromCodeCoverage]
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        logger.LogInformation(CLIENT_DISCONNECTED, Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
    #endregion
}
