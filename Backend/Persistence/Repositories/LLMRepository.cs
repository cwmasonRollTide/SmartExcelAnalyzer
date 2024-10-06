using Persistence.Models;
using Microsoft.Extensions.Options;

namespace Persistence.Repositories;

public interface ILLMRepository
{
    Task<string> QueryLLM(string document_id, string question);
    Task<float[]?> ComputeEmbedding(string documentId, string question);
}

public class LLMRepository(IOptions<LLMServiceOptions> options, IWebRepository<string> queryRepo, IWebRepository<float[]?> computeRepo) : ILLMRepository
{
    private readonly LLMServiceOptions _llmOptions = options.Value;
    private readonly IWebRepository<string> _queryRepo = queryRepo;
    private readonly IWebRepository<float[]?> _computeRepo = computeRepo;

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
    public async Task<string> QueryLLM(string document_id, string question) => 
        await _queryRepo.PostAsync(_llmOptions.LLM_SERVICE_URL + "/query", new { document_id, question });

    /// <summary>
    /// Compute the embedding of a given text
    /// Calls the /compute_embedding endpoint of the LLM service
    /// Returns a vector which represents the text as interpreted by the LLM model
    /// </summary>
    /// <param name="document_id"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public async Task<float[]?> ComputeEmbedding(string document_id, string data) => 
        await _computeRepo.PostAsync(_llmOptions.LLM_SERVICE_URL + "/compute_embedding", new { document_id, data });
}
