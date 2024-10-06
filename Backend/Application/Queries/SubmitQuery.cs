using MediatR;
using FluentValidation;
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
    }
}

public class SubmitQuery : IRequest<string>
{
    public required string Query { get; set; }
    public required string DocumentId { get; set; }
}

public class SubmitQueryHandler(ILLMRepository lLMRepository) : IRequestHandler<SubmitQuery, string>
{
    private readonly ILLMRepository _lLMRepository = lLMRepository;

    public async Task<string> Handle(SubmitQuery request, CancellationToken cancellationToken) 
    {
        var result = await _lLMRepository.QueryLLM(request.DocumentId, request.Query);
        return result;
    }
}