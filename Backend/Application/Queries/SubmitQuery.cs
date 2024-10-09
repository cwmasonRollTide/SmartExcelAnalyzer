using MediatR;
using FluentValidation;
using Domain.Persistence.DTOs;
using Persistence.Repositories;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Application.Queries;

public class SubmitQueryValidator : AbstractValidator<SubmitQuery>
{
    public SubmitQueryValidator()
    {
        RuleFor(x => x.Query).NotNull().WithMessage("Query is required.");
        RuleFor(x => x.Query).NotEmpty().WithMessage("Query is required.");
        RuleFor(x => x.DocumentId).NotNull().WithMessage("DocumentId is required.");
        RuleFor(x => x.DocumentId).NotEmpty().WithMessage("DocumentId is required.");
        RuleFor(x => x.RelevantRowsCount)
            .GreaterThanOrEqualTo(0).WithMessage("RelevantRowsCount must be greater than or equal to 0.")
            .When(x => x.RelevantRowsCount.HasValue);
    }
}

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
    ILLMRepository llmRepository,
    ILogger<SubmitQueryHandler> logger,
    IVectorDbRepository vectorDbRepository
) : IRequestHandler<SubmitQuery, QueryAnswer?>
{
    #region Log Message Constants
    private const string LogQueryingLLM = "Querying LLM for query {Query} and documentId {DocumentId}.";
    private const string LogComputingEmbedding = "Computing embedding for query {Query}.";
    private const string LogQueryingVectorDb = "Querying VectorDb for the most relevant rows for query {Query} and documentId {DocumentId}.";
    private const string LogFailedToQueryLLM = "Failed to query LLM for query {Query} and documentId {DocumentId}.";
    private const string LogFailedToComputeEmbedding = "Failed to compute embedding for query {Query}.";
    private const string LogFailedToQueryVectorDb = "Failed to query VectorDb for the most relevant rows for query {Query} and documentId {DocumentId}.";
    private const string LogRelevantRowsCount = "Returning {RelevantRowsCount} relevant rows.";
    private const string LogQueryLLMSuccess = "Query {Query} was successful. Answer: {Answer}";
    #endregion

    #region Dependencies
    private readonly ILogger<SubmitQueryHandler> _logger = logger;
    private readonly ILLMRepository _llmRepository = llmRepository;
    private readonly IVectorDbRepository _vectorDbRepository = vectorDbRepository;
    #endregion

    #region Handle Method
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
    public async Task<QueryAnswer?> Handle(SubmitQuery request, CancellationToken cancellationToken = default) 
    {
        _logger.LogInformation(LogQueryingLLM, request.Query, request.DocumentId);
        var result = await _llmRepository.QueryLLM(request.Query, request.DocumentId, cancellationToken);
        if (result is null)
        {
            _logger.LogWarning(LogFailedToQueryLLM, request.Query, request.DocumentId);
            return null;
        }
        if (request.RelevantRowsCount.HasValue) // We want more than ten rows of the data used to answer the query
        {
            _logger.LogInformation(LogComputingEmbedding, request.Query);
            var embedding = await _llmRepository.ComputeEmbedding(text: request.Query, cancellationToken);
            if (embedding is null)
            {
                _logger.LogWarning(LogFailedToComputeEmbedding, request.Query);
                return null;
            }
            _logger.LogInformation(LogQueryingVectorDb, request.Query, request.DocumentId);
            var vectorResponse = await _vectorDbRepository.QueryVectorData(
                queryVector: embedding!, 
                documentId: request.DocumentId, 
                cancellationToken: cancellationToken,
                topRelevantCount: (int)request.RelevantRowsCount!
            );
            if (vectorResponse is null)
            {
                _logger.LogWarning(LogFailedToQueryVectorDb, request.Query, request.DocumentId);
                return null;
            }
            result.RelevantRows = vectorResponse.Rows!;
        }
        _logger.LogInformation(LogQueryLLMSuccess, request.Query, result.Answer);
        return result;
    }
    #endregion
}
