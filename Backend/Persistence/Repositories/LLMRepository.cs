using Domain.Persistence.DTOs;
using Microsoft.Extensions.Options;
using Domain.Persistence.Configuration;

namespace Persistence.Repositories;

public interface ILLMRepository
{
    Task<float[]?> ComputeEmbedding(string text, CancellationToken cancellationToken = default);
    Task<QueryAnswer> QueryLLM(string document_id, string question, CancellationToken cancellationToken = default);
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
public class LLMRepository(IOptions<LLMServiceOptions> options, IWebRepository<float[]?> computeService, IWebRepository<QueryAnswer> queryService) : ILLMRepository
{
    #region Service URLs
    private string QUERY_URL => _llmOptions.LLM_SERVICE_URL + "/query";
    private string COMPUTE_URL => _llmOptions.LLM_SERVICE_URL + "/compute_embedding";
    #endregion

    #region Dependencies
    /// <summary>
    /// Options for the LLM service
    /// Contains the URL of the LLM service
    /// </summary>
    private readonly LLMServiceOptions _llmOptions = options.Value;
    /// <summary>
    /// Web repository for querying the LLM model
    /// </summary>
    private readonly IWebRepository<QueryAnswer> _queryService = queryService;
    /// <summary>
    /// Web repository for calling the compute function of the LLM model
    /// </summary>
    private readonly IWebRepository<float[]?> _computeService = computeService;
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

    #endregion
}
