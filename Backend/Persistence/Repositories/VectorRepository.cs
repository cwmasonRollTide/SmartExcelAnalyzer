using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Persistence.Database;
using Domain.Persistence.DTOs;
using Domain.Persistence.Configuration;

namespace Persistence.Repositories;

public interface IVectorDbRepository
{
    Task<string> SaveDocumentAsync(SummarizedExcelData vectorSpreadsheetData, CancellationToken cancellationToken = default);
    Task<SummarizedExcelData> QueryVectorData(string documentId, float[] queryVector, int topRelevantCount = 10, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for interacting with the VectorDb database.
/// This repository is responsible for saving and querying documents from the VectorDb database.
/// </summary>
/// <param name="databaseWrapper"></param>
/// <param name="logger"></param>
/// <param name="llmRepository"></param>
/// <param name="llmOptions"></param>
public class VectorRepository(
    IDatabaseWrapper databaseWrapper,
    ILogger<VectorRepository> logger,
    ILLMRepository llmRepository,
    IOptions<LLMServiceOptions> llmOptions
) : IVectorDbRepository
{
    #region Logging Message Constants
    private const string LOG_START_SAVE = "Starting to save document to the database.";
    private const string LOG_FAIL_SAVE_VECTORS = "Failed to save vectors of the document to the database.";
    private const string LOG_FAIL_SAVE_SUMMARY = "Failed to save the summary of the document with Id {Id} to the database.";
    private const string LOG_SUCCESS_SAVE = "Saved document with id {DocumentId} to the database.";
    private const string LOG_START_QUERY = "Querying the VectorDb for the most relevant rows for document {DocumentId}.";
    private const string LOG_FAIL_QUERY_ROWS = "Failed to query the relevant rows of the document with Id {Id} from the database.";
    private const string LOG_FAIL_QUERY_SUMMARY = "Failed to query the summary of the document with Id {Id} from the database.";
    private const string LOG_SUCCESS_QUERY = "Querying the VectorDb for the most relevant rows for document {DocumentId} was successful. Found {RelevantRowsCount} relevant rows.";
    private const string LOG_START_COMPUTE = "Computing embeddings for document with {Count} rows.";
    private const string LOG_COMPUTE_EMBEDDINGS = "Computed {Count} embeddings in {ElapsedMilliseconds}ms";
    private const string LOG_NULL_EMBEDDING = "Embedding at index {Index} is null.";
    private const string LOG_FAIL_SAVE_BATCH = "Failed to save vectors to the database.";
    private const string LOG_FAIL_SAVE_BATCH_FOR_DOCUMENT = "Failed to save vectors to the database for document {DocumentId}.";
    private const string LOG_INCONSISTENT_IDS = "Inconsistent document IDs across batches. Document Id {DocumentId} is not equal to batch document Id {BatchDocumentId}.";
    #endregion

    #region Dependencies
    private readonly ILogger<VectorRepository> _logger = logger;
    private readonly IDatabaseWrapper _database = databaseWrapper;
    private readonly ILLMRepository _llmRepository = llmRepository;
    private readonly int BatchSize = llmOptions.Value.COMPUTE_BATCH_SIZE;
    #endregion

    #region Public Methods
    /// <summary>
    /// Saves the document to the database.
    /// Computes the embeddings of the rows in batches and stores them in the database.
    /// </summary>
    /// <param name="vectorSpreadsheetData"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string> SaveDocumentAsync(SummarizedExcelData vectorSpreadsheetData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(LOG_START_SAVE);
        var documentId = await ProcessRowsInBatches(vectorSpreadsheetData.Rows!, cancellationToken);
        if (documentId is null)
        {
            _logger.LogWarning(LOG_FAIL_SAVE_VECTORS);
            return null!;
        }
        var summarySuccess = await _database.StoreSummaryAsync(documentId, vectorSpreadsheetData.Summary!, cancellationToken);
        if (summarySuccess < 0) _logger.LogWarning(LOG_FAIL_SAVE_SUMMARY, documentId);
        _logger.LogInformation(LOG_SUCCESS_SAVE, documentId);
        return documentId;
    }

    /// <summary>
    /// Queries the VectorDb for the most relevant rows for a given document and query vector.
    /// Compares the query vector to the embeddings of the rows in the database and returns the most relevant rows.
    /// </summary>
    /// <param name="documentId"></param>
    /// <param name="queryVector"></param>
    /// <param name="topRelevantCount"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SummarizedExcelData> QueryVectorData(string documentId, float[] queryVector, int topRelevantCount = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(LOG_START_QUERY, documentId);
        var relevantDocuments = await _database.GetRelevantDocumentsAsync(documentId, queryVector, topRelevantCount, cancellationToken);
        if (relevantDocuments is null)
        {
            _logger.LogWarning(LOG_FAIL_QUERY_ROWS, documentId);
            return new() { Summary = null!, Rows = null! };
        }
        var rows = new ConcurrentBag<ConcurrentDictionary<string, object>>(relevantDocuments);
        var summary = await _database.GetSummaryAsync(documentId, cancellationToken);
        if (summary.IsEmpty)
        {
            _logger.LogWarning(LOG_FAIL_QUERY_SUMMARY, documentId);
            return new() { Summary = null!, Rows = rows };
        }
        _logger.LogInformation(LOG_SUCCESS_QUERY, documentId, relevantDocuments.Count());
        return new()
        {
            Rows = rows,
            Summary = summary
        };
    }
    #endregion

    #region Private Concurrent Data Processing Methods
    /// <summary>
    /// Processes the rows in batches and computes the embeddings for each batch.
    /// Splits up the task into the producer/consumer model to reduce the time taken to compute the embeddings and store them in the db
    /// </summary>
    /// <param name="rows"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<string> ProcessRowsInBatches(ConcurrentBag<ConcurrentDictionary<string, object>> rows, CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<(IEnumerable<float[]> Embeddings, ConcurrentBag<ConcurrentDictionary<string, object>> Batch)>();
        var batches = CreateBatches(rows);
        var producerTask = ProduceEmbeddings(batches, channel, cancellationToken);
        var consumerTask = ConsumeAndStoreEmbeddings(channel, cancellationToken);
        await Task.WhenAll(producerTask, consumerTask);
        return consumerTask.Result;
    }

    /// <summary>
    /// Splits the rows into batches of size BatchSize.
    /// </summary>
    /// <param name="rows"></param>
    /// <returns></returns>
    private ConcurrentBag<ConcurrentBag<ConcurrentDictionary<string, object>>> CreateBatches(ConcurrentBag<ConcurrentDictionary<string, object>> rows) => new(
        rows.Select((row, index) => new { Row = row, Index = index })
            .GroupBy(x => x.Index / BatchSize)
            .Select(g => new ConcurrentBag<ConcurrentDictionary<string, object>>(g.Select(x => x.Row))));

    /// <summary>
    /// Produces embeddings for each batch of rows and writes them to the channel.
    /// Oh boy back in school baby this is that parallel programming stuff we learned 
    /// about with the threads and the locks and the semaphores and the mutexes and the deadlocks 
    /// and the forks and the eating with the philosophers and what not
    /// </summary>
    /// <param name="batches"></param>
    /// <param name="channel"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task ProduceEmbeddings(
        ConcurrentBag<ConcurrentBag<ConcurrentDictionary<string, object>>> batches, 
        Channel<(
            IEnumerable<float[]> Embeddings,
            ConcurrentBag<ConcurrentDictionary<string, object>> Batch
        )> channel, 
        CancellationToken cancellationToken = default
    )
    {
        var options = new JsonSerializerOptions { WriteIndented = false };
        var tasks = batches.Select(async batch =>
        {
            _logger.LogInformation(LOG_START_COMPUTE, batch.Count);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var embeddings = await _llmRepository.ComputeBatchEmbeddings(batch.Select(row => JsonSerializer.Serialize(row, options)), cancellationToken);
            stopwatch.Stop();
            _logger.LogInformation(LOG_COMPUTE_EMBEDDINGS, batch.Count, stopwatch.ElapsedMilliseconds);
            if (embeddings is null)
            {
                _logger.LogWarning(LOG_FAIL_SAVE_BATCH);
                return;
            }
            await channel.Writer.WriteAsync((embeddings, batch)!, cancellationToken);
        });
        await Task.WhenAll(tasks);
        channel.Writer.Complete();
    }

    private async Task<string> ConsumeAndStoreEmbeddings(
        Channel<(
            IEnumerable<float[]> Embeddings,
            ConcurrentBag<ConcurrentDictionary<string, object>> Batch
        )> channel, 
        CancellationToken cancellationToken = default
    )
    {
        string documentId = null!;
        var batchesToStore = new ConcurrentBag<ConcurrentDictionary<string, object>>();
        await foreach (var (embeddings, batch) in channel.Reader.ReadAllAsync(cancellationToken))
        {
            ProcessBatch(embeddings, batch, batchesToStore, cancellationToken);
            if (batchesToStore.Count >= BatchSize * 5)
            {
                documentId = await StoreBatch(batchesToStore, documentId, cancellationToken);
                batchesToStore.Clear();
            }
        }
        if (!batchesToStore.IsEmpty) await _database.StoreVectorsAsync(batchesToStore, documentId, cancellationToken);
        return documentId;
    }

    private void ProcessBatch(
        IEnumerable<float[]> embeddings,
        ConcurrentBag<ConcurrentDictionary<string, object>> batch, 
        ConcurrentBag<ConcurrentDictionary<string, object>> batchesToStore,
        CancellationToken cancellationToken = default
    )
    {
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cancellationToken };
        var batchArray = batch.ToArray();
        var embeddingsArray = embeddings.ToArray();
        Parallel.For(0, batchArray.Length, parallelOptions, index =>
        {
            var row = batchArray[index];
            var embedding = embeddingsArray[index];
            if (embedding != null)
                row["embedding"] = embedding;
            else
                _logger.LogWarning(LOG_NULL_EMBEDDING, index);
            batchesToStore.Add(row);
        });
    }

    private async Task<string> StoreBatch(
        ConcurrentBag<ConcurrentDictionary<string, object>> batchRowsToStore, string documentId, 
        CancellationToken cancellationToken
    )
    {
        var batchDocumentId = await _database.StoreVectorsAsync(batchRowsToStore, documentId, cancellationToken);
        if (batchDocumentId is null)
        {
            _logger.LogWarning(LOG_FAIL_SAVE_BATCH_FOR_DOCUMENT, documentId);
            return documentId;
        }
        documentId ??= batchDocumentId;
        if (documentId != batchDocumentId)
            _logger.LogWarning(LOG_INCONSISTENT_IDS, documentId, batchDocumentId);

        return documentId;
    }
    #endregion
}