namespace Persistence.Hubs;

public interface IProgressHubWrapper
{
    public Task SendProgress(double parseProgress, double saveProgress);
}
