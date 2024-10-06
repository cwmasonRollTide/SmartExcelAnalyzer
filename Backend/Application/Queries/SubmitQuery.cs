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

public class SubmitQuery : IRequest<List<Dictionary<string, object>>>
{
    public required string Query { get; set; }
    public required string DocumentId { get; set; }
}

public class SubmitQueryHandler(IVectorDbRepository vectorDbRepository) : IRequestHandler<SubmitQuery, List<Dictionary<string, object>>>
{
    private readonly IVectorDbRepository _vectorDbRepository = vectorDbRepository;

    public async Task<List<Dictionary<string, object>>> Handle(SubmitQuery request, CancellationToken cancellationToken) 
    {
        
        

    }
}