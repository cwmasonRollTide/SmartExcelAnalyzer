using System.Text.Json;
using Persistence.Database;
using Domain.Persistence.DTOs;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using Domain.Persistence.Configuration;

namespace Persistence.Repositories;

public interface IVectorDbRepository
{
    Task<string> SaveDocumentAsync(
        SummarizedExcelData vectorSpreadsheetData, 
        CancellationToken cancellationToken = default
    );

    Task<SummarizedExcelData> QueryVectorData(
        string documentId, 
        float[] queryVector, 
        int topRelevantCount = 10, 
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// Repository for interacting with the VectorDb database.
/// This repository is responsible for saving and querying documents from the VectorDb database.
/// </summary>
/// <param name="databaseWrapper"></param>
/// <param name="logger"></param>
/// <param name="llmRepository"></param>
/// <param name="llmOptions"></param>
/// <param name="databaseOptions"></param>
public class VectorRepository(
    IDatabaseWrapper databaseWrapper,
    ILogger<VectorRepository> logger,
    ILLMRepository llmRepository,
    IOptions<LLMServiceOptions> llmOptions,
    IOptions<DatabaseOptions> databaseOptions
) : IVectorDbRepository
{
    #region Logging Message Constants
    private const string LOG_NULL_INPUT_DATA = "Input data is null.";
    private const string LOG_ERROR_CREATING_BATCHES = "Error creating batches";
    private const string LOG_NULL_DOCUMENT_ID = "Document ID is null or empty.";
    private const string LOG_ERROR_STORING_EMBEDDINGS = "Error storing embeddings";
    private const string LOG_EMPTY_QUERY_VECTOR = "Query vector is empty or null.";
    private const string LOG_NULL_EMBEDDING = "Embedding at index {Index} is null.";    
    private const string LOG_ERROR_COMPUTING_EMBEDDINGS = "Error computing embeddings";
    private const string LOG_START_SAVE = "Starting to save document to the database.";
    private const string LOG_BATCH_CREATION_CANCELLED = "Batch creation was cancelled.";
    private const string LOG_FAIL_SAVE_BATCH = "Failed to save vectors to the database.";
    private const string LOG_STORING_EMBEDDINGS_CANCELLED = "Storing embeddings was cancelled.";
    private const string LOG_SUCCESS_SAVE = "Saved document with id {DocumentId} to the database.";
    private const string LOG_START_COMPUTE = "Computing embeddings for document with {Count} rows.";
    private const string LOG_EMBEDDING_COMPUTATION_CANCELLED = "Embedding computation was cancelled.";
    private const string LOG_FAIL_SAVE_VECTORS = "Failed to save vectors of the document to the database.";
    private const string LOG_COMPUTE_EMBEDDINGS = "Computed {Count} embeddings in {ElapsedMilliseconds}ms";
    private const string LOG_START_QUERY = "Querying the VectorDb for the most relevant rows for document {DocumentId}.";
    private const string LOG_FAIL_SAVE_SUMMARY = "Failed to save the summary of the document with Id {Id} to the database.";
    private const string LOG_FAIL_QUERY_SUMMARY = "Failed to query the summary of the document with Id {Id} from the database.";
    private const string LOG_FAIL_SAVE_BATCH_FOR_DOCUMENT = "Failed to save vectors to the database for document {DocumentId}.";
    private const string LOG_ERROR_QUERYING_VECTOR_DATA = "An error occurred while querying vector data for document {DocumentId}";
    private const string LOG_FAIL_QUERY_ROWS = "Failed to query the relevant rows of the document with Id {Id} from the database.";
    private const string LOG_INCONSISTENT_IDS = "Inconsistent document IDs across batches. Document Id {DocumentId} is not equal to batch document Id {BatchDocumentId}.";
    private const string LOG_SUCCESS_QUERY = "Querying the VectorDb for the most relevant rows for document {DocumentId} was successful. Found {RelevantRowsCount} relevant rows.";
    #endregion

    #region Dependencies
    private readonly ILogger<VectorRepository> _logger = logger;
    private readonly IDatabaseWrapper _database = databaseWrapper;
    private readonly ILLMRepository _llmRepository = llmRepository;
    private readonly int _computeEmbeddingBatchSize = llmOptions.Value.COMPUTE_BATCH_SIZE;
    private readonly int _maxConcurrentTasks = databaseOptions.Value.MAX_CONNECTION_COUNT;
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
        if (vectorSpreadsheetData == null)
        {
            _logger.LogWarning(LOG_NULL_INPUT_DATA);
            return null!;
        }
        _logger.LogInformation(LOG_START_SAVE);
        var documentId = await SaveDocumentDataAsync(vectorSpreadsheetData.Rows ?? [], cancellationToken);
        if (documentId is null)
        {
            _logger.LogWarning(LOG_FAIL_SAVE_VECTORS);
            return null!;
        }
        if (vectorSpreadsheetData.Summary != null)
        {
            var summarySuccess = await _database.StoreSummaryAsync(documentId, vectorSpreadsheetData.Summary, cancellationToken);
            if (summarySuccess < 0) _logger.LogWarning(LOG_FAIL_SAVE_SUMMARY, documentId);
        }
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
        if (string.IsNullOrEmpty(documentId))
        {
            _logger.LogWarning(LOG_NULL_DOCUMENT_ID);
            return null!;
        }

        if (queryVector == null || queryVector.Length == 0)
        {
            _logger.LogWarning(LOG_EMPTY_QUERY_VECTOR);
            return null!;
        }

        _logger.LogInformation(LOG_START_QUERY, documentId);
        try
        {
            var relevantDocuments = await _database.GetRelevantDocumentsAsync(documentId, queryVector, topRelevantCount, cancellationToken);
            if (relevantDocuments == null || !relevantDocuments.Any())
            {
                _logger.LogWarning(LOG_FAIL_QUERY_ROWS, documentId);
                return null!;
            }
            var rows = new ConcurrentBag<ConcurrentDictionary<string, object>>(relevantDocuments);
            var summary = await _database.GetSummaryAsync(documentId, cancellationToken);
            if (summary == null || summary.IsEmpty)
            {
                _logger.LogWarning(LOG_FAIL_QUERY_SUMMARY, documentId);
                return new SummarizedExcelData { Summary = null!, Rows = rows };
            }
            _logger.LogInformation(LOG_SUCCESS_QUERY, documentId, relevantDocuments.Count());
            return new SummarizedExcelData
            {
                Rows = rows,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LOG_ERROR_QUERYING_VECTOR_DATA, documentId);
            return null!;
        }
    }
    #endregion

    #region Private Data Processing Methods
    /// <summary>
    /// Processes the rows in batches and computes the embeddings for each batch.
    /// Splits up the task into the producer/consumer model to reduce the time taken to compute the embeddings and store them in the db
    /// </summary>
    /// <param name="rows"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<string> SaveDocumentDataAsync(ConcurrentBag<ConcurrentDictionary<string, object>> rows, CancellationToken cancellationToken)
    {
        var batchChannel = Channel.CreateBounded<IEnumerable<ConcurrentDictionary<string, object>>>(new BoundedChannelOptions(Environment.ProcessorCount - 4)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
        var embeddingChannel = Channel.CreateBounded<(IEnumerable<float[]> Embeddings, IEnumerable<ConcurrentDictionary<string, object>> Batch)>(new BoundedChannelOptions(Environment.ProcessorCount - 4)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
        var createBatchesTask = CreateBatchesAsync(rows, batchChannel.Writer, cancellationToken);
        var computeEmbeddingsTask = ComputeEmbeddingsAsync(batchChannel.Reader, embeddingChannel.Writer, cancellationToken);
        var storeEmbeddingsTask = StoreEmbeddingsAsync(embeddingChannel.Reader, cancellationToken);
        await createBatchesTask;
        await computeEmbeddingsTask;
        return await storeEmbeddingsTask;
    }

    private async Task CreateBatchesAsync(
        ConcurrentBag<ConcurrentDictionary<string, object>> rows,
        ChannelWriter<IEnumerable<ConcurrentDictionary<string, object>>> writer,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var batch = new ConcurrentBag<ConcurrentDictionary<string, object>>();
            await Parallel.ForEachAsync(rows, cancellationToken, async (row, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                batch.Add(row);
                if (batch.Count.Equals(_computeEmbeddingBatchSize))
                {
                    var batchToWrite = new ConcurrentBag<ConcurrentDictionary<string, object>>(batch);
                    batch.Clear();
                    await writer.WriteAsync(batchToWrite, cancellationToken);
                }
            });

            if (!batch.IsEmpty) await writer.WriteAsync(batch, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(LOG_BATCH_CREATION_CANCELLED);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LOG_ERROR_CREATING_BATCHES);
        }
        finally
        {
            writer.Complete();
        }
    }

    private async Task ComputeEmbeddingsAsync(
        ChannelReader<IEnumerable<ConcurrentDictionary<string, object>>> reader,
        ChannelWriter<(IEnumerable<float[]>, IEnumerable<ConcurrentDictionary<string, object>>)> writer,
        CancellationToken cancellationToken
    )
    {
        var serializerOptions =  new JsonSerializerOptions { WriteIndented = false };
        try
        {
            await foreach (var batch in reader.ReadAllAsync(cancellationToken))
            {
                _logger.LogInformation(LOG_START_COMPUTE, batch.Count());
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var embeddings = await _llmRepository.ComputeBatchEmbeddings(batch.Select(row => JsonSerializer.Serialize(row, serializerOptions)), cancellationToken);
                stopwatch.Stop();
                _logger.LogInformation(LOG_COMPUTE_EMBEDDINGS, batch.Count(), stopwatch.ElapsedMilliseconds);
                if (embeddings is not null)
                    await writer.WriteAsync((embeddings, batch)!, cancellationToken);
                else
                    _logger.LogWarning(LOG_FAIL_SAVE_BATCH);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(LOG_EMBEDDING_COMPUTATION_CANCELLED);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LOG_ERROR_COMPUTING_EMBEDDINGS);
        }
        finally
        {
            writer.Complete();
        }
    }

    private async Task<string> StoreEmbeddingsAsync(
        ChannelReader<(
            IEnumerable<float[]> Embeddings, 
            IEnumerable<ConcurrentDictionary<string, object>> Batch
        )> reader,
        CancellationToken cancellationToken)
    {
        string documentId = null!;
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
        try
        {
            await foreach (var (embeddings, batch) in reader.ReadAllAsync(cancellationToken))
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var batchesToStore = new ConcurrentBag<ConcurrentDictionary<string, object>>();
                    await ProcessBatchAsync(embeddings, batch, batchesToStore, cancellationToken);
                    documentId = await SaveBatchOfRowEmbeddings(batchesToStore, documentId, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(LOG_STORING_EMBEDDINGS_CANCELLED);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LOG_ERROR_STORING_EMBEDDINGS);
        }
        return documentId;
    }

    private async Task ProcessBatchAsync(
        IEnumerable<float[]> embeddings,
        IEnumerable<ConcurrentDictionary<string, object>> batch,
        ConcurrentBag<ConcurrentDictionary<string, object>> batchesToStore,
        CancellationToken cancellationToken)
    {
        var pairs = batch.Zip(embeddings, (row, embedding) => (row, embedding));
        await Parallel.ForEachAsync(pairs, new ParallelOptions 
        { 
            MaxDegreeOfParallelism = _maxConcurrentTasks,
            CancellationToken = cancellationToken
        }, 
        async (pair, ct) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            var (row, embedding) = pair;
            if (embedding != null)
                row["embedding"] = embedding;
            else
                _logger.LogWarning(LOG_NULL_EMBEDDING, "Unknown");
            batchesToStore.Add(row);
        });
    }

    private async ValueTask<string> SaveBatchOfRowEmbeddings(
        ConcurrentBag<ConcurrentDictionary<string, object>> batchOfRowsToSave, 
        string documentId,
        CancellationToken cancellationToken)
    {
        var batchDocumentId = await _database.StoreVectorsAsync(batchOfRowsToSave, documentId, cancellationToken);
        if (batchDocumentId is null)
        {
            _logger.LogWarning(LOG_FAIL_SAVE_BATCH_FOR_DOCUMENT, documentId);
            return documentId;
        }
        documentId ??= batchDocumentId;
        if (documentId != batchDocumentId) _logger.LogWarning(LOG_INCONSISTENT_IDS, documentId, batchDocumentId);

        return documentId;
    }
    #endregion
}
