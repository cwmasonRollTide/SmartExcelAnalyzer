using FluentValidation;

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
