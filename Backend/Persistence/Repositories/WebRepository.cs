using System.Text;
using Newtonsoft.Json;

namespace Persistence.Repositories;

public interface IWebService<T>
{
    Task<T> PostAsync(string endpoint, object payload, CancellationToken cancellationToken = default);
}

public class WebService<T>(IHttpClientFactory httpClientFactory) : IWebService<T>
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    public async Task<T> PostAsync(string endpoint, object payload, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonConvert.DeserializeObject<T>(result)!;
    }
}