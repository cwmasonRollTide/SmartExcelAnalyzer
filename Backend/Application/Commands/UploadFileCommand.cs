using MediatR;
using FluentValidation;
using Application.Services;
using Persistence.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Commands;

/// <summary>
/// Validates the uploadfilecommand request
/// Ensures that the file is not null and has a length greater than 0
/// Also must be smaller than 15MB
/// </summary>
public class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileCommandValidator()
    {
        RuleFor(x => x.File).NotNull().WithMessage("File is required.");
        RuleFor(x => x.File).Must(file => file is not null && file.Length > 0).WithMessage("File is empty.");
        RuleFor(x => x.File).Must(file => file is not null && file.Length < 15 * 1024 * 1024).WithMessage("File size must be less than 15 MB.");
    }
}

public class UploadFileCommand : IRequest<string?>
{
    public IFormFile? File { get; set; }
}

/// <summary>
/// UploadFileCommandHandler handles the UploadFileCommand request
/// dependencies: IExcelFileService, IVectorDbRepository, ILogger<UploadFileCommandHandler>
/// Returns the documentId of the uploaded file if successful, otherwise null
/// </summary>
/// <param name="excelService"></param>
/// <param name="vectorDbRepository"></param>
public class UploadFileCommandHandler(
    IExcelFileService excelService,
    IVectorDbRepository vectorDbRepository,
    ILogger<UploadFileCommandHandler> logger
) : IRequestHandler<UploadFileCommand, string?>
{
    #region Log Message Constants
    private const string LogPreparingExcelFile = "Preparing excel file {FileName} for LLM.";
    private const string LogSavingDocument = "Saving file {Filename} to the vector database.";
    private const string LogFailedToPrepareExcelFile = "Failed to prepare excel file {FileName} for LLM.";
    private const string LogFailedSavingVectorDb = "Failed to save file {Filename} to the vector database.";
    private const string LogSavedDocumentSuccess = "Success: Saved file {Filename} with id {DocumentId} to the vector database.";
    private const string LogTimeParseTaken = "Time taken to prepare excel file: {Time}ms";
    private const string LogTimeSaveTaken = "Time taken to save document: {Time}ms";
    #endregion

    #region Dependencies
    private readonly IExcelFileService _excelService = excelService;
    private readonly ILogger<UploadFileCommandHandler> _logger = logger;
    private readonly IVectorDbRepository _vectorDbRepository = vectorDbRepository;
    #endregion

    #region Handle Method
    /// <summary>
    /// Handles the UploadFileCommand request. Prepares the excel file for the LLM and saves it to the vector database.
    /// Parses the excel file and computes the embedding of each row with the LLM.
    /// Saves the way the LLM computes the embedding of all the rows of each excel file in the database
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// DocumentId? string. If it is null, the operation failed either at the parsing stage
    /// or the saving to the vector db stage
    /// </returns>
    public async Task<string?> Handle(UploadFileCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogTrace(LogPreparingExcelFile, request.File!.FileName);
        var stopwatch = Stopwatch.StartNew();
        var summarizedExcelData = await _excelService.PrepareExcelFileForLLMAsync(file: request.File, cancellationToken);
        stopwatch.Stop();
        _logger.LogTrace(LogTimeParseTaken, stopwatch.ElapsedMilliseconds);
        if (summarizedExcelData is null)
        {
            _logger.LogInformation(LogFailedToPrepareExcelFile, request.File.FileName);
            return null;
        }
        _logger.LogTrace(LogSavingDocument, request.File.FileName);
        stopwatch.Restart();
        var documentId = await _vectorDbRepository.SaveDocumentAsync(summarizedExcelData, cancellationToken);
        stopwatch.Stop();
        _logger.LogTrace(LogTimeSaveTaken, stopwatch.ElapsedMilliseconds);
        if (documentId is null)  
        {
            _logger.LogInformation(LogFailedSavingVectorDb, request.File.FileName);
            return null;
        }
        _logger.LogInformation(LogSavedDocumentSuccess, request.File.FileName, documentId);
        return documentId;
    }
    #endregion
}