using MediatR;
using FluentValidation;
using Application.Services;
using Domain.Persistence.DTOs;
using Persistence.Repositories;
using Microsoft.AspNetCore.Http;

namespace Application.Commands;

public class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileCommandValidator()
    {
        RuleFor(x => x.File).NotNull().WithMessage("File is required.");
        RuleFor(x => x.File.Length).GreaterThan(0).WithMessage("File is empty.");
    }
}

public class UploadFileCommand : IRequest<string?>
{
    public required IFormFile File { get; set; }
}

/// <summary>
/// UploadFileCommandHandler handles the UploadFileCommand request
/// dependencies: IExcelFileService, IVectorDbRepository
/// Returns the documentId of the uploaded file if successful, otherwise null
/// </summary>
/// <param name="excelService"></param>
/// <param name="vectorDbRepository"></param>
public class UploadFileCommandHandler(
    IExcelFileService excelService,
    IVectorDbRepository vectorDbRepository
) : IRequestHandler<UploadFileCommand, string?>
{
    private readonly IExcelFileService _excelService = excelService;
    private readonly IVectorDbRepository _vectorDbRepository = vectorDbRepository;

    public async Task<string?> Handle(
        UploadFileCommand request, 
        CancellationToken cancellationToken = default
    )
    {
        var ( Rows, Summary ) = await _excelService.PrepareExcelFileForLLMAsync(request.File, cancellationToken);
        if (Rows is null || Summary is null) return null;
        var documentId = await _vectorDbRepository.SaveDocumentAsync(
            new VectorQueryResponse 
            {
                Summary = Summary,
                RelevantRows = Rows
            }, cancellationToken
        );
        return documentId;
    }
}