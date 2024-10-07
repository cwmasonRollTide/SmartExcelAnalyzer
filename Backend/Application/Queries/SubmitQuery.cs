using MediatR;
using FluentValidation;
using Domain.Persistence.DTOs;
using Persistence.Repositories;

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

public class SubmitQuery : IRequest<QueryAnswer>
{
    public required string Query { get; set; }
    public required string DocumentId { get; set; }
    public int? RelevantRowsCount { get; set; } = null;
}

public class SubmitQueryHandler(
    ILLMRepository llmRepository, 
    IVectorDbRepository vectorDbRepository
) : IRequestHandler<SubmitQuery, QueryAnswer>
{
    private readonly ILLMRepository _llmRepository = llmRepository;
    private readonly IVectorDbRepository _vectorDbRepository = vectorDbRepository;

    /// <summary>
    /// Handles the SubmitQuery request. Asks
    /// 
    /// dependencies: ILLMRepository, IVectorDbRepository
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<QueryAnswer> Handle(
        SubmitQuery request, 
        CancellationToken cancellationToken = default
    ) 
    {
        var result = await _llmRepository.QueryLLM(
            question: request.Query, 
            document_id: request.DocumentId, 
            cancellationToken: cancellationToken
        );
        if (request.RelevantRowsCount.HasValue)
        {
            var embedding = await _llmRepository.ComputeEmbedding(
                text: request.Query, 
                cancellationToken
            );
            var vectorResponse = await _vectorDbRepository.QueryVectorData(
                queryVector: embedding!, 
                documentId: request.DocumentId, 
                cancellationToken: cancellationToken,
                topRelevantCount: (int)request.RelevantRowsCount!
            );
            result.RelevantRows = vectorResponse.RelevantRows;
        }
        return result;
    }
}
