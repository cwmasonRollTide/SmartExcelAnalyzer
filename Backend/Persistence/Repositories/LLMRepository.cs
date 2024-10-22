using Domain.Persistence.DTOs;
using Microsoft.Extensions.Options;
using Persistence.Repositories.API;
using Domain.Persistence.Configuration;

namespace Persistence.Repositories;

public interface ILLMRepository
{
    Task<float[]?> ComputeEmbedding(string text, CancellationToken cancellationToken = default);
    Task<QueryAnswer> QueryLLM(string document_id, string question, CancellationToken cancellationToken = default);
    Task<IEnumerable<float[]?>> ComputeBatchEmbeddings(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for interfacing with the LLM service
/// This repository is responsible for querying the LLM model
/// and computing the embeddings of text
/// It uses the WebRepository to make HTTP requests to the LLM service
/// The LLM service is a REST API which provides endpoints for querying the model
/// and computing the embeddings of text
/// The LLM service is a separate service which is responsible for running the LLM model (in python server)
/// </summary>
/// <param name="options">
///     Options for the LLM service
/// </param>
/// <param name="queryService">
///     Web repository for querying the LLM model - specifically the /query endpoint
/// </param>
/// <param name="computeService">
///     Web repository for computing the embeddings of text - specifically the /compute_embedding endpoint
/// </param>
public class LLMRepository(
    ILLMServiceLoadBalancer llmServiceLoadBalancer,
    IWebRepository<float[]?> computeService, 
    IWebRepository<IEnumerable<float[]?>> batchComputeService, 
    IWebRepository<QueryAnswer> queryService
) : ILLMRepository
{
    #region Service URLs
    private string QUERY_URL => _llmServiceLoadBalancer.GetServiceUrl() + "/query";
    private string COMPUTE_URL => _llmServiceLoadBalancer.GetServiceUrl() + "/compute_embedding";
    private string COMPUTE_BATCH_URL => _llmServiceLoadBalancer.GetServiceUrl() + "/compute_batch_embedding";
    #endregion

    #region Dependencies
    /// <summary>
    /// Web repository for querying the LLM model
    /// </summary>
    private readonly IWebRepository<QueryAnswer> _queryService = queryService;
    /// <summary>
    /// Web repository for calling the compute function of the LLM model
    /// </summary>
    private readonly IWebRepository<float[]?> _computeService = computeService;
        /// <summary>
    /// Load balancer for the LLM service URLs 
    /// </summary>
    private readonly ILLMServiceLoadBalancer _llmServiceLoadBalancer = llmServiceLoadBalancer;
    /// <summary>
    /// Web repository for calling the batch compute function of the LLM model
    /// </summary>
    private readonly IWebRepository<IEnumerable<float[]?>> _batchComputeService = batchComputeService;
    #endregion

    #region Public Methods
    /// <summary>
    /// Query the LLM model with a given document_id and question
    /// Calls the /query endpoint of the LLM service
    /// Returns the answer to the question as interpreted by the LLM model
    /// Given the question provided, the LLM will use the most relevant rows 
    /// from the excel sheet to answer the question
    /// </summary>
    /// <param name="document_id">
    ///     The document_id of the excel sheet which contains the data
    /// </param>
    /// <param name="question">
    ///     The question to ask the LLM model
    /// </param>
    /// <returns>
    ///     QueryAnswer. The answer to the question as interpreted by the LLM model
    /// </returns>
    public async Task<QueryAnswer> QueryLLM(string document_id, string question, CancellationToken cancellationToken = default) =>    
        await _queryService.PostAsync(QUERY_URL, new { document_id, question }, cancellationToken);

    /// <summary>
    /// Compute the embedding of a given text
    /// Calls the /compute_embedding endpoint of the LLM service
    /// Returns a vector which represents the text or data as interpreted by the LLM model
    /// </summary>
    /// <param name="document_id"></param>
    /// <param name="text"></param>
    /// <returns>
    /// Vector representing the text or data
    /// </returns>
    public async Task<float[]?> ComputeEmbedding(string text, CancellationToken cancellationToken = default) => 
        await _computeService.PostAsync(COMPUTE_URL, new { text }, cancellationToken);

    /// <summary>
    /// Compute the embeddings of a batch of texts
    /// Calls the /compute_embedding endpoint of the LLM service with many
    /// texts at once to reduce repetitive calls, batch up calls
    /// </summary>
    /// <param name="texts"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IEnumerable<float[]?>> ComputeBatchEmbeddings(IEnumerable<string> texts, CancellationToken cancellationToken = default) => 
        await _batchComputeService.PostAsync(COMPUTE_BATCH_URL, new { texts = texts.ToList() }, cancellationToken);
    #endregion
}

public interface ILLMServiceLoadBalancer
{
    string GetServiceUrl();
}

public class LLMLoadBalancer(IOptions<LLMServiceOptions> options) : ILLMServiceLoadBalancer
{
    private int _currentIndex = 0;
    private readonly object _lock = new();
    private readonly List<string> _serviceUrls = options.Value.LLM_SERVICE_URLS;

    /// <summary>
    /// Get the next service URL in the list of service URLs
    /// </summary>
    /// <returns></returns>
    public string GetServiceUrl()
    {
        lock (_lock)
        {
            if (_currentIndex >= _serviceUrls.Count) _currentIndex = 0;
            return _serviceUrls[_currentIndex++];
        }
    }
}