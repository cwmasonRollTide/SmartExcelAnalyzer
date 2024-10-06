using Persistence.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;

namespace Persistence.Repositories;

public interface ILLMRepository
{
    Task<float[]?> ComputeEmbedding(string documentId, string text);
    // Task<string> ProcessEmbeddingAsync(string documentId, string question);
}

public class LLMRepository(IHttpClientFactory httpClientFactory, IOptions<LLMServiceOptions> options) : ILLMRepository
{
    private readonly LLMServiceOptions _options = options.Value;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    /// <summary>
    /// Compute the embedding of the given text using the LLM service
    /// Sends a post request to the LLM service with the text and returns the embedding as a float array
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    // public async Task<float[]> QueryLLM(string text)
    // {
    //     var response = await _httpClientFactory.CreateClient().PostAsync(_options.LLM_SERVICE_URL, new StringContent(text));
    //     response.EnsureSuccessStatusCode();
    //     var embeddingJson = await response.Content.ReadAsStringAsync();
    //     return JsonSerializer.Deserialize<float[]>(embeddingJson) ?? throw new InvalidOperationException("Failed to deserialize embedding");
    // }

    public async Task<float[]?> ComputeEmbedding(string document_id, string question)
    {
        var client = _httpClientFactory.CreateClient();
        var query = new { document_id, question };
        var content = new StringContent(JsonConvert.SerializeObject(query), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(_options.LLM_SERVICE_URL + "/compute_embedding", content);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<float[]>(result);
    }
}
