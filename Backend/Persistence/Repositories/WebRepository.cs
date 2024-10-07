using System.Text;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace Persistence.Repositories;

public interface IWebRepository<T>
{
    Task<T> PostAsync(string endpoint, object payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// A generic repository for making web requests
/// This repository is responsible for making HTTP requests to a given endpoint
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="httpClientFactory"></param>
[ExcludeFromCodeCoverage]
public class WebRepository<T>(IHttpClientFactory httpClientFactory) : IWebRepository<T>
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    /// <summary>
    /// Make a POST request to the given endpoint with the given payload
    /// Deserialize the response to the given type
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="payload"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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