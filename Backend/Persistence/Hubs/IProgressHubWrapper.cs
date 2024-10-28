namespace Persistence.Hubs;

public interface IProgressHubWrapper
{
    Task SendError(string message);
    Task SendCompletion(string message);
    Task SendProgress(double progress, double total);
}
