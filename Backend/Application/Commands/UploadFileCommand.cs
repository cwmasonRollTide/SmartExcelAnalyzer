using MediatR;
using Persistence.Hubs;
using System.Diagnostics;
using Application.Services;
using Domain.Persistence.DTOs;
using Persistence.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using System.ComponentModel.DataAnnotations;

namespace Application.Commands;

public class UploadFileCommand : IRequest<string?>
{
    [Required]
    public IFormFile File { get; set; } = null!;    
    public IProgress<(double ParseProgress, double SaveProgress)> Progress { get; set; } = new Progress<(double, double)>();
}

/// <summary>
/// The service responsible for handling Excel file operations.
/// UploadFileCommandHandler handles the UploadFileCommand request
/// </summary>
/// <param name="excelService">Service for processing Excel files</param>
/// <param name="vectorDbRepository">Repository for vector database operations</param>
/// <param name="logger">Logger for operation tracking</param>
/// <param name="hubContext">SignalR hub context for reporting progress to the client on parsing and saving</param>
public class UploadFileCommandHandler(
    IExcelFileService _excelService,
    IVectorDbRepository _vectorDbRepository,
    ILogger<UploadFileCommandHandler> _logger,
    IHubContext<ProgressHub> _hubContext
) : IRequestHandler<UploadFileCommand, string?>
{
    #region Log Message Constants
    private const string SignalRMethod = "ReceiveProgress";
    private const string LogTimeSaveTaken = "Time taken to save document: {Time}ms";
    private const string LogTimeParseTaken = "Time taken to prepare excel file: {Time}ms";
    private const string LogPreparingExcelFile = "Preparing excel file {FileName} for LLM.";
    private const string LogSavingDocument = "Saving file {Filename} to the vector database.";
    private const string LogFailedToPrepareExcelFile = "Failed to prepare excel file {FileName} for LLM.";
    private const string LogFailedSavingVectorDb = "Failed to save file {Filename} to the vector database.";
    private const string LogSavedDocumentSuccess = "Success: Saved file {Filename} with id {DocumentId} to the vector database.";
    #endregion

    /// <summary>
    /// Handles the UploadFileCommand request. Prepares the excel file for the LLM and saves it to the vector database.
    /// Parses the excel file and computes the embedding of each row with the LLM.
    /// Saves the way the LLM computes the embedding of all the rows of each excel file in the database
    /// </summary>
    /// <param name="request">The upload file command containing the file and progress tracker</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>
    /// DocumentId? string. If it is null, the operation failed either at the parsing stage
    /// or the saving to the vector db stage
    /// </returns>
    public async Task<string?> Handle(
        UploadFileCommand request, 
        CancellationToken cancellationToken = default
    )
    {
        var progress = new Progress<(double, double)>(
            async report =>
                await _hubContext
                    .Clients
                    .All
                    .SendAsync(
                        method: SignalRMethod, 
                        report.Item1, 
                        report.Item2, 
                        cancellationToken: cancellationToken
                    )
        );
        var summarizedExcelData = await PrepareExcelFileAsync(
            file: request.File!, 
            progress: progress, 
            cancellationToken: cancellationToken
        );
        if (summarizedExcelData is null) return null;
        return await SaveToVectorDatabaseAsync(
            data: summarizedExcelData, 
            fileName: request.File!.FileName, 
            progress: progress, 
            cancellationToken: cancellationToken
        );
    }

    private async Task<SummarizedExcelData?> PrepareExcelFileAsync(
        IFormFile file, 
        IProgress<(double, double)> progress, 
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogTrace(LogPreparingExcelFile, file.FileName);
        var stopwatch = Now;
        var summarizedExcelData = await _excelService.PrepareExcelFileForLLMAsync(
            file: file, 
            progress: progress, 
            cancellationToken: cancellationToken
        );
        stopwatch.Stop();
        _logger.LogTrace(LogTimeParseTaken, stopwatch.ElapsedMilliseconds);
        if (summarizedExcelData is null) _logger.LogInformation(LogFailedToPrepareExcelFile, file.FileName);
        return summarizedExcelData;
    }

    private async Task<string?> SaveToVectorDatabaseAsync(
        SummarizedExcelData data, 
        string fileName, 
        IProgress<(double, double)> progress, 
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogTrace(LogSavingDocument, fileName);
        var stopwatch = Now;
        var documentId = await _vectorDbRepository.SaveDocumentAsync(
            data, 
            progress, 
            cancellationToken
        );           
        stopwatch.Stop();
        _logger.LogTrace(LogTimeSaveTaken, stopwatch.ElapsedMilliseconds);

        if (documentId is null)
            _logger.LogInformation(LogFailedSavingVectorDb, fileName);
        else
            _logger.LogInformation(LogSavedDocumentSuccess, fileName, documentId);

        return documentId;
    }

    private static Stopwatch Now => Stopwatch.StartNew();
}