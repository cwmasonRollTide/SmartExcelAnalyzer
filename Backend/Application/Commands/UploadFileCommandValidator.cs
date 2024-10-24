using FluentValidation;

namespace Application.Commands;

public class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileCommandValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("File is required.");

        RuleFor(x => x.File)
            .Must(file => file?.Length > 0)
            .When(x => x.File != null)
            .WithMessage("File is empty.");
    }
}
