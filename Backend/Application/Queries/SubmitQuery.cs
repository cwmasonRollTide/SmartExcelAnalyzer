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
        RuleFor(x => x.RelevantRowsCount).GreaterThanOrEqualTo(0).WithMessage("RelevantRowsCount must be greater than or equal to 0.");
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
    /// Handles the SubmitQuery request
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<QueryAnswer> Handle(
        SubmitQuery request, 
        CancellationToken cancellationToken = default) 
    {
        var result = await _llmRepository.QueryLLM(request.DocumentId, request.Query, cancellationToken);
        if (request.RelevantRowsCount.HasValue)
        {
            var embedding = await _llmRepository.ComputeEmbedding(request.DocumentId, request.Query, cancellationToken);
            if (embedding == null) return result;
            var vectorResponse = await _vectorDbRepository.QueryVectorData(
                documentId: request.DocumentId, 
                queryVector: embedding, 
                topRelevantCount: (int)request.RelevantRowsCount!, 
                cancellationToken: cancellationToken
            );
            result.RelevantRows = vectorResponse.RelevantRows;
        }
        return result;
    }
}
