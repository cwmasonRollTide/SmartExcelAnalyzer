
using Domain.Persistence.DTOs;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Persistence.Repositories;

public interface IVectorDbRepository
{
    Task<string> SaveDocumentAsync(SummarizedExcelData vectorSpreadsheetData, CancellationToken cancellationToken = default);
    Task<SummarizedExcelData> QueryVectorData(string documentId, float[] queryVector, int topRelevantCount = 10, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for interfacing with the VectorDb repository
/// This repository is responsible for storing and querying the vectors of documents
/// It uses the LLM repository to compute the embeddings of the documents
/// The VectorDb repository is a database which stores the vectors of the documents
/// </summary>
/// <param name="lLMRepository"></param>
/// <param name="context"></param>
/// <param name="logger"></param>
public class VectorDbRepository(IDatabaseWrapper databaseWrapper, ILogger<VectorDbRepository> logger) : IVectorDbRepository
{
    #region Log Message Constants
    private const string LogStartingSaveDocument = "Starting to save document to the database.";
    private const string LogSavedDocumentSuccess = @"Saved document with id {DocumentId} to the database.";
    private const string LogFailedToSaveDocument = "Failed to save vectors of the document to the database.";
    private const string LogFailedToSaveSummary = @"Failed to save the summary of the document with Id {Id} to the database.";
    private const string LogQueryingVectorDb = "Querying the VectorDb for the most relevant rows for document {DocumentId}.";
    private const string LogQueryingSummaryFailed = "Failed to query the summary of the document with Id {Id} from the database.";
    private const string LogQueryingDocumentsFailed = "Failed to query the relevant rows of the document with Id {Id} from the database.";
    private const string LogQueryingVectorDbSuccess = "Querying the VectorDb for the most relevant rows for document {DocumentId} was successful. Found {RelevantRowsCount} relevant rows.";
    #endregion

    #region Dependencies
    private readonly IDatabaseWrapper _database = databaseWrapper;
    private readonly ILogger<VectorDbRepository> _logger = logger;
    #endregion

    #region Public Methods
    /// <summary>
    /// Save the document to the database.
    /// The document is represented as a list of rows, where each row is a dictionary of column names and values.
    /// The document is stored in the database as a list of vectors, where each vector is the embedding of a row.
    /// The summary is stored in the database as a dictionary of summary statistics.
    /// </summary>
    /// <param name="vectorSpreadsheetData">RelevantRows, and Summary</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string> SaveDocumentAsync(SummarizedExcelData vectorSpreadsheetData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(LogStartingSaveDocument);
        var documentId = await _database.StoreVectorsAsync(vectorSpreadsheetData.Rows!, cancellationToken);
        if (documentId is null) 
        {
            _logger.LogWarning(LogFailedToSaveDocument);
            return null!;
        }
        var summarySuccess = await _database.StoreSummaryAsync(documentId, vectorSpreadsheetData.Summary!, cancellationToken);
        if (summarySuccess < 0) 
        {
            _logger.LogWarning(LogFailedToSaveSummary, documentId);
            return documentId!;
        }
        _logger.LogInformation(LogSavedDocumentSuccess, documentId);
        return documentId;
    }

    /// <summary>
    /// Query the database for the most relevant rows to a given query vector.
    /// </summary>
    /// <param name="documentId"></param>
    /// <param name="queryVector"></param>
    /// <param name="topRelevantCount"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SummarizedExcelData> QueryVectorData(
        string documentId, 
        float[] queryVector, 
        int topRelevantCount = 10,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(LogQueryingVectorDb, documentId);
        var relevantDocuments = await _database.GetRelevantDocumentsAsync(documentId, queryVector, topRelevantCount, cancellationToken);
        if (relevantDocuments is null)
        {
            _logger.LogWarning(LogQueryingDocumentsFailed, documentId);
            return new() { Summary = null!, Rows = null! };
        }
        var rows = new ConcurrentBag<ConcurrentDictionary<string, object>>(relevantDocuments);
        var summary = await _database.GetSummaryAsync(documentId, cancellationToken);
        if (summary.IsEmpty)
        {
            _logger.LogWarning(LogQueryingSummaryFailed, documentId);
            return new() { Summary = null!, Rows =  rows };
        }
        _logger.LogInformation(LogQueryingVectorDbSuccess, documentId, relevantDocuments.Count());
        return new()
        {
            Rows = rows,
            Summary = summary
        };
    }
    #endregion
}
