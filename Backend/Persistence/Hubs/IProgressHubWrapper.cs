namespace Persistence.Hubs;

public interface IProgressHubWrapper
{
    Task SendError(string message);
    Task SendProgress(double progress, double total);
}
