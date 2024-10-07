
using System.Text.Json;
using Domain.Utilities;
using Domain.Persistence;
using Domain.Persistence.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public interface IVectorDbRepository
{
    Task<string> SaveDocumentAsync(SummarizedExcelData vectorSpreadsheetData, CancellationToken cancellationToken = default);
    Task<SummarizedExcelData> QueryVectorData(string documentId, float[] queryVector, int topRelevantCount = 10, CancellationToken cancellationToken = default);
}

public class VectorDbRepository(ILLMRepository lLMRepository, ApplicationDbContext context, ILogger<VectorDbRepository> logger) : IVectorDbRepository
{
    #region Log Message Constants
    private const string LogStartingSaveDocument = "Starting to save document to the database.";
    private const string LogSavedDocumentSuccess = @"Saved document with id {DocumentId} to the database.";
    private const string LogFailedToSaveDocument = "Failed to save vectors of the document to the database.";
    private const string LogFailedToSaveSummary = "Failed to save the summary of the document to the database.";
    #endregion

    #region Dependencies
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<VectorDbRepository> _logger = logger;
    private readonly ILLMRepository _llmRepository = lLMRepository;
    private readonly JsonSerializerOptions _serializerSettings = new();
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
        var documentId = await StoreVectors(vectorSpreadsheetData.RelevantRows, cancellationToken);
        if (documentId is null) 
        {
            _logger.LogWarning(LogFailedToSaveDocument);
            return null!;
        }
        var summarySuccess = await StoreSummary(documentId, vectorSpreadsheetData.Summary, cancellationToken);
        if (!summarySuccess.HasValue) 
        {
            _logger.LogWarning(LogFailedToSaveSummary);
            return null!;
        }
        _logger.LogInformation(LogSavedDocumentSuccess, documentId);
        return documentId;
    }

    public async Task<SummarizedExcelData> QueryVectorData(
        string documentId, 
        float[] queryVector, 
        int topRelevantCount = 10,
        CancellationToken cancellationToken = default
    )
    {
        var relevantDocuments = await _context.Documents
            .Where(d => d.Id == documentId)
            .OrderByDescending(d => VectorMath.CalculateSimilarity(d.Embedding, queryVector!))
            .Take(topRelevantCount)
            .ToListAsync(cancellationToken);
        var relevantRows = relevantDocuments
            .Select(d => JsonSerializer.Deserialize<Dictionary<string, object>>(d.Content, _serializerSettings)!)
            .ToList();
        var summary = await _context.Summaries
            .Where(s => s.Id == documentId)
            .Select(s => JsonSerializer.Deserialize<Dictionary<string, object>>(s.Content, _serializerSettings))
            .FirstOrDefaultAsync(cancellationToken) ?? [];
        return new()
        {
            Summary = summary,
            RelevantRows = relevantRows
        };
    }
    #endregion

    private async Task<string?> StoreVectors(
        List<Dictionary<string, object>> rows, 
        CancellationToken cancellationToken = default
    )
    {
        var documentId = Guid.NewGuid().ToString();
        var documentTasks = rows.Select(row => GenerateDocument(documentId, row, cancellationToken)).ToList();
        var documents = await Task.WhenAll(documentTasks);
        _context.Documents.AddRange(documents);
        var result = await _context.SaveChangesAsync(cancellationToken);
        if (result <= 0) return null!;
        return documentId;
    }

    private async Task<int?> StoreSummary(
        string documentId, 
        Dictionary<string, object> summary, 
        CancellationToken cancellationToken = default
    )
    {
        _context.Summaries.Add(new Summary { Id = documentId, Content = JsonSerializer.Serialize(summary) });
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Generate a document from a row of data.
    /// The document is represented as a dictionary of column names and values. Full row of values column/value pairs
    /// </summary>
    /// <param name="documentId"></param>
    /// <param name="row"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<Document> GenerateDocument(
        string documentId, 
        Dictionary<string, object> row, 
        CancellationToken cancellationToken = default
    ) 
    {
        var serializedData = JsonSerializer.Serialize(row);
        var embedding = await _llmRepository.ComputeEmbedding(
            serializedData, 
            cancellationToken
        );
        return new()
        {
            Id = documentId,
            Embedding = embedding!,
            Content = serializedData
        };
    }
}
