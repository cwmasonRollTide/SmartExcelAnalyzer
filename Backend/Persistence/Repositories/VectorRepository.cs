using Persistence.Database;
using Domain.Persistence.DTOs;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace Persistence.Repositories;

public class VectorDbRepository : IVectorDbRepository
{
    private const int BatchSize = 100; // Adjust this based on your needs
    private readonly IDatabaseWrapper _database;
    private readonly ILogger<VectorDbRepository> _logger;
    private readonly ILLMRepository _llmRepository;

    public VectorDbRepository(IDatabaseWrapper databaseWrapper, ILogger<VectorDbRepository> logger, ILLMRepository llmRepository)
    {
        _database = databaseWrapper;
        _logger = logger;
        _llmRepository = llmRepository;
    }

    public async Task<string> SaveDocumentAsync(SummarizedExcelData vectorSpreadsheetData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting to save document to the database.");

        var documentId = await ProcessRowsInBatches(vectorSpreadsheetData.Rows!, cancellationToken);

        if (documentId is null)
        {
            _logger.LogWarning("Failed to save vectors of the document to the database.");
            return null!;
        }

        var summarySuccess = await _database.StoreSummaryAsync(documentId, vectorSpreadsheetData.Summary!, cancellationToken);
        if (summarySuccess < 0)
        {
            _logger.LogWarning("Failed to save the summary of the document with Id {Id} to the database.", documentId);
            return documentId!;
        }

        _logger.LogInformation("Saved document with id {DocumentId} to the database.", documentId);
        return documentId;
    }

    private async Task<string> ProcessRowsInBatches(ConcurrentBag<ConcurrentDictionary<string, object>> rows, CancellationToken cancellationToken)
    {
        var batches = rows
            .Select((row, index) => new { Row = row, Index = index })
            .GroupBy(x => x.Index / BatchSize)
            .Select(g => g.Select(x => x.Row).ToList())
            .ToList();

        string documentId = null!;
        foreach (var batch in batches)
        {
            var batchRows = new ConcurrentBag<ConcurrentDictionary<string, object>>();
            var embeddings = await _llmRepository.ComputeEmbeddings(batch.Select(JsonConvert.SerializeObject), cancellationToken);

            for (int i = 0; i < batch.Count; i++)
            {
                var row = batch[i];
                var embedding = embeddings.ElementAtOrDefault(i);
                if (embedding != null)
                {
                    row["embedding"] = embedding;
                }
                else
                {
                    _logger.LogWarning("Embedding at index {Index} is null.", i);
                }
                batchRows.Add(row);
            }

            var batchDocumentId = await _database.StoreVectorsAsync(batchRows, cancellationToken);
            if (batchDocumentId is null)
            {
                _logger.LogWarning("Failed to save a batch of vectors to the database.");
                return null!;
            }

            if (documentId is null)
            {
                documentId = batchDocumentId;
            }
            else if (documentId != batchDocumentId)
            {
                _logger.LogWarning("Inconsistent document IDs across batches.");
                return null!;
            }
        }

        return documentId;
    }

    public async Task<SummarizedExcelData> QueryVectorData(string documentId, float[] queryVector, int topRelevantCount = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Querying the VectorDb for the most relevant rows for document {DocumentId}.", documentId);
        var relevantDocuments = await _database.GetRelevantDocumentsAsync(documentId, queryVector, topRelevantCount, cancellationToken);
        if (relevantDocuments is null)
        {
            _logger.LogWarning("Failed to query the relevant rows of the document with Id {Id} from the database.", documentId);
            return new() { Summary = null!, Rows = null! };
        }
        var rows = new ConcurrentBag<ConcurrentDictionary<string, object>>(relevantDocuments);
        var summary = await _database.GetSummaryAsync(documentId, cancellationToken);
        if (summary.IsEmpty)
        {
            _logger.LogWarning("Failed to query the summary of the document with Id {Id} from the database.", documentId);
            return new() { Summary = null!, Rows =  rows };
        }
        _logger.LogInformation("Querying the VectorDb for the most relevant rows for document {DocumentId} was successful. Found {RelevantRowsCount} relevant rows.", documentId, relevantDocuments.Count());
        return new()
        {
            Rows = rows,
            Summary = summary
        };
    }
}

public interface IVectorDbRepository
{
    Task<string> SaveDocumentAsync(SummarizedExcelData vectorSpreadsheetData, CancellationToken cancellationToken = default);
    Task<SummarizedExcelData> QueryVectorData(string documentId, float[] queryVector, int topRelevantCount = 10, CancellationToken cancellationToken = default);
}