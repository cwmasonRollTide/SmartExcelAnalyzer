using MediatR;
using Domain.Persistence.DTOs;
using Persistence.Repositories;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Application.Queries;

public class SubmitQuery : IRequest<QueryAnswer?>
{
    [Required]
    public required string Query { get; set; }
    [Required]
    public required string DocumentId { get; set; }
    public int? RelevantRowsCount { get; set; } = null;
}

/// <summary>
/// SubmitQueryHandler handles the SubmitQuery request
/// dependencies: ILLMRepository, IVectorDbRepository, ILogger
/// Returns the answer to the query and possibly the most relevant rows
/// Possibly returning more rows than the default 10 if requested
/// 
/// The handler queries the LLM for the answer to the query and computes the embedding of the query.
/// If the relevantRowsCount is provided, it also queries the VectorDb for the most relevant rows.
/// 
/// </summary>
/// <param name="llmRepository"></param>
/// <param name="logger"></param>
/// <param name="vectorDbRepository"></param>
public class SubmitQueryHandler(
    ILLMRepository _llmRepository,
    ILogger<SubmitQueryHandler> _logger,
    IVectorDbRepository _vectorDbRepository
) : IRequestHandler<SubmitQuery, QueryAnswer?>
{
    #region Log Message Constants
    private const string LogComputingEmbedding = "Computing embedding for query {Query}.";
    private const string LogQueryLLMSuccess = "Query {Query} was successful. Answer: {Answer}";
    private const string LogFailedToComputeEmbedding = "Failed to compute embedding for query {Query}.";
    private const string LogQueryingLLM = "Querying LLM for query {Query} and documentId {DocumentId}.";
    private const string LogFailedToQueryLLM = "Failed to query LLM for query {Query} and documentId {DocumentId}.";
    private const string LogQueryingVectorDb = "Querying VectorDb for the most relevant rows for query {Query} and documentId {DocumentId}.";
    private const string LogFailedToQueryVectorDb = "Failed to query VectorDb for the most relevant rows for query {Query} and documentId {DocumentId}.";
    #endregion

    #region Handle
    /// <summary>
    /// Handles the SubmitQuery request. Asks the LLM to process the query and returns the answer.
    /// If the relevantRowsCount is provided, it also asks the VectorDb to return the most relevant rows.
    /// Possibly returning more rows than the default 10 if requested
    /// dependencies: ILLMRepository, IVectorDbRepository, ILogger
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    ///     QueryAnswer. The answer to the query and possibly the most relevant rows
    /// </returns>
    public async Task<QueryAnswer?> Handle(
        SubmitQuery request, 
        CancellationToken cancellationToken = default
    ) 
    {
        var result = await QueryLLMAsync(request, cancellationToken);
        if (result is null) return null;
        if (ShouldEnrichResponse(request.RelevantRowsCount)) await EnrichWithRelevantRowsAsync(request, result, cancellationToken);
        _logger.LogInformation(LogQueryLLMSuccess, request.Query, result.Answer);
        return result;
    }

    /// <summary>
    /// Queries the LLM for the answer to the query and returns the result.     
    /// If the LLM returns null, it logs a warning.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    ///     QueryAnswer. The answer to the query
    /// </returns>
                
    private async Task<QueryAnswer?> QueryLLMAsync(
        SubmitQuery request, 
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(LogQueryingLLM, request.Query, request.DocumentId);
        var result = await _llmRepository.QueryLLM(
            document_id: request.DocumentId, 
            question: request.Query, 
            cancellationToken
        );
        if (result is null) _logger.LogWarning(LogFailedToQueryLLM, request.Query, request.DocumentId);
        return result;
    }

    private static bool ShouldEnrichResponse(int? relevantRowsCount) => relevantRowsCount.HasValue;

    private async Task EnrichWithRelevantRowsAsync(
        SubmitQuery request, 
        QueryAnswer result, 
        CancellationToken cancellationToken = default
    )
    {
        var embedding = await ComputeEmbeddingAsync(request, cancellationToken);
        if (embedding is null) return;
        var vectorResponse = await QueryVectorDbAsync(request, embedding, cancellationToken);
        if (vectorResponse is not null) result.RelevantRows = vectorResponse.Rows!;
    }

    private async Task<SummarizedExcelData?> QueryVectorDbAsync(
        SubmitQuery request, 
        float[] embedding, 
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(LogQueryingVectorDb, request.Query, request.DocumentId);
        var vectorResponse = await _vectorDbRepository.QueryVectorData(
            queryVector: embedding,
            documentId: request.DocumentId,
            cancellationToken: cancellationToken,
            topRelevantCount: (int)request.RelevantRowsCount!
        );
        if (vectorResponse is null) _logger.LogWarning(LogFailedToQueryVectorDb, request.Query, request.DocumentId);
        return vectorResponse;
    }

    private async Task<float[]?> ComputeEmbeddingAsync(
        SubmitQuery request, 
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(LogComputingEmbedding, request.Query);
        var embedding = await _llmRepository.ComputeEmbedding(text: request.Query, cancellationToken);
        if (embedding is null) _logger.LogWarning(LogFailedToComputeEmbedding, request.Query);
        return embedding;
    }
    #endregion
}
