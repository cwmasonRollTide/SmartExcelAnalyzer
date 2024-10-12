using Microsoft.Extensions.Options;
using Domain.Persistence.Configuration;

namespace Persistence.Repositories.API;

public interface ILLMServiceLoadBalancer
{
    string GetNextServiceUrl();
}

public class LLMServiceLoadBalancer(IOptions<LLMServiceOptions> options) : ILLMServiceLoadBalancer
{
    private int _currentIndex = 0;
    private readonly object _lock = new();
    private readonly List<string> _serviceUrls = options.Value.LLM_SERVICE_URLS;

    /// <summary>
    /// Get the next service URL in the list of service URLs
    /// </summary>
    /// <returns></returns>
    public string GetNextServiceUrl()
    {
        lock (_lock)
        {
            if (_currentIndex >= _serviceUrls.Count) _currentIndex = 0;
            return _serviceUrls[_currentIndex++];
        }
    }
}
