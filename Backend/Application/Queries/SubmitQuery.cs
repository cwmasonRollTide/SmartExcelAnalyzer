using MediatR;
using FluentValidation;
using Persistence.Repositories;
using Persistence.Models.DTOs;

namespace Application.Queries;

public class SubmitQueryValidator : AbstractValidator<SubmitQuery>
{
    public SubmitQueryValidator()
    {
        RuleFor(x => x.Query).NotNull().WithMessage("Query is required.");
        RuleFor(x => x.Query).NotEmpty().WithMessage("Query is required.");
        RuleFor(x => x.DocumentId).NotNull().WithMessage("DocumentId is required.");
        RuleFor(x => x.DocumentId).NotEmpty().WithMessage("DocumentId is required.");
    }
}

public class SubmitQuery : IRequest<QueryAnswer>
{
    public required string Query { get; set; }
    public required string DocumentId { get; set; }
}

public class SubmitQueryHandler(ILLMRepository lLMRepository) : IRequestHandler<SubmitQuery, QueryAnswer>
{
    private readonly ILLMRepository _lLMRepository = lLMRepository;

    public async Task<QueryAnswer> Handle(SubmitQuery request, CancellationToken cancellationToken) 
    {
        var result = await _lLMRepository.QueryLLM(request.DocumentId, request.Query, cancellationToken);
        
        return result;
    }
}