using Persistence.Models;
using Persistence.Models.DTOs;
using Microsoft.Extensions.Options;

namespace Persistence.Repositories;

public interface ILLMRepository
{
    Task<QueryAnswer> QueryLLM(string document_id, string question, CancellationToken cancellationToken = default);
    Task<float[]?> ComputeEmbedding(string documentId, string question, CancellationToken cancellationToken = default);
}

public class LLMRepository(IOptions<LLMServiceOptions> options, IWebService<QueryAnswer> queryService, IWebService<float[]?> computeService) : ILLMRepository
{
    private readonly LLMServiceOptions _llmOptions = options.Value;
    private readonly IWebService<QueryAnswer> _queryService = queryService;
    private readonly IWebService<float[]?> _computeService = computeService;

    /// <summary>
    /// Query the LLM model with a given document_id and question
    /// Calls the /query endpoint of the LLM service
    /// Returns the answer to the question as interpreted by the LLM model
    /// Given the question provided, the LLM will use the most relevant rows 
    /// from the excel sheet to answer the question
    /// </summary>
    /// <param name="document_id"></param>
    /// <param name="question"></param>
    /// <returns></returns>
    public async Task<QueryAnswer> QueryLLM(string document_id, string question, CancellationToken cancellationToken = default) => 
        await _queryService.PostAsync(_llmOptions.LLM_SERVICE_URL + "/query", new { document_id, question }, cancellationToken);

    /// <summary>
    /// Compute the embedding of a given text
    /// Calls the /compute_embedding endpoint of the LLM service
    /// Returns a vector which represents the text or data as interpreted by the LLM model
    /// </summary>
    /// <param name="document_id"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public async Task<float[]?> ComputeEmbedding(string document_id, string data, CancellationToken cancellationToken = default) => 
        await _computeService.PostAsync(_llmOptions.LLM_SERVICE_URL + "/compute_embedding", new { document_id, data }, cancellationToken);
}
