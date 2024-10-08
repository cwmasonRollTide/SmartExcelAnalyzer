using System.Text;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace Persistence.Repositories;

public interface IWebRepository<T>
{
    Task<T> PostAsync(string endpoint, object payload, CancellationToken cancellationToken = default);
}

[ExcludeFromCodeCoverage]
public class WebRepository<T>(IHttpClientFactory httpClientFactory) : IWebRepository<T>
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<T> PostAsync(string endpoint, object payload, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("DefaultClient");
        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonConvert.DeserializeObject<T>(result)!;
    }
}