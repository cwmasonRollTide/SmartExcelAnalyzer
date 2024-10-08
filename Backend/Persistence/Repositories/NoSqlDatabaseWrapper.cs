using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Persistence.Database;
using Microsoft.Extensions.Options;
using Domain.Persistence.Configuration;

namespace Persistence.Repositories;

public class NoSqlDatabaseWrapper(IMongoDatabase database, IOptions<DatabaseOptions> options, ILogger<NoSqlDatabaseWrapper> logger) : IDatabaseWrapper
{
    private readonly IMongoDatabase _database = database;
    private readonly IOptions<DatabaseOptions> _options = options;
    private readonly ILogger<NoSqlDatabaseWrapper> _logger = logger;
    private readonly JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true };

    private int BatchSize => _options.Value.SAVE_BATCH_SIZE;
    private const int MaxConcurrentTasks = 4;//TODO: Make this configurable
    private const int MaxRetries = 3;//TODO: Make this configurable

    /// <summary>
    /// StoreVectorsAsync stores the vectors in the database in batches.
    /// </summary>
    /// <param name="rows"></param>
    /// <param name="docId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string> StoreVectorsAsync(IEnumerable<ConcurrentDictionary<string, object>> rows, string? docId = default, CancellationToken cancellationToken = default)
    {
        var collection = _database.GetCollection<BsonDocument>("documents");
        var documentId = docId ?? ObjectId.GenerateNewId().ToString();
        using var semaphore = new SemaphoreSlim(MaxConcurrentTasks);
        await Task.WhenAll(CreateBatches(rows).Select(batch => ProcessBatchAsync(batch, collection, documentId, semaphore, cancellationToken)));
        return documentId;
    }

    private IEnumerable<IEnumerable<ConcurrentDictionary<string, object>>> CreateBatches(IEnumerable<ConcurrentDictionary<string, object>> rows) =>
        rows.Select((row, index) => new { Row = row, Index = index })
            .GroupBy(x => x.Index / BatchSize)
            .Select(g => g.Select(x => x.Row));

    /// <summary>
    /// ProcessBatchAsync processes a batch of documents to be inserted into the collection.
    /// Converts the embedding array into a BsonArray for MongoDB and inserts the batch into the collection.
    /// </summary>
    /// <param name="batch"></param>
    /// <param name="collection"></param>
    /// <param name="documentId"></param>
    /// <param name="semaphore"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task ProcessBatchAsync(
        IEnumerable<ConcurrentDictionary<string, object>> batch, 
        IMongoCollection<BsonDocument> collection, 
        string documentId,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken = default)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            var batchDocuments = CreateBatchDocuments(batch, documentId);
            await InsertBatchWithRetryAsync(collection, batchDocuments, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// CreateBatchDocuments creates the documents for the batch to be inserted into the collection.
    /// Converts the embedding array into a BsonArray for MongoDB.
    /// </summary>
    /// <param name="batch"></param>
    /// <param name="documentId"></param>
    /// <returns></returns>
    private static IEnumerable<BsonDocument> CreateBatchDocuments(IEnumerable<ConcurrentDictionary<string, object>> batch, string documentId) =>
        batch.Select(row =>
        {
            return new BsonDocument
            {
                { "_id", documentId },
                { "content", BsonDocument.Parse(JsonSerializer.Serialize(row)) },
                { "embedding", new BsonArray((row["embedding"] as float[]) ?? []) }
            };
        });

    /// <summary>
    /// InsertBatchWithRetryAsync inserts a batch of documents into the collection with retries.
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="batchDocuments"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private async Task InsertBatchWithRetryAsync(IMongoCollection<BsonDocument> collection, IEnumerable<BsonDocument> batchDocuments, CancellationToken cancellationToken)
    {
        var retryCount = 0;
        while (retryCount < MaxRetries)
        {
            try
            {
                await collection.InsertManyAsync(batchDocuments, cancellationToken: cancellationToken);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await HandleInsertErrorAsync(ex, retryCount);
                retryCount++;
            }
        }
        throw new Exception($"Failed to insert batch after {MaxRetries} attempts.");
    }

    private async Task HandleInsertErrorAsync(Exception ex, int retryCount)
    {
        _logger.LogWarning(ex, "Failed to insert batch. Retry attempt: {RetryCount}", retryCount + 1);
        await Task.Delay(1000 * (retryCount + 1)); // Exponential backoff
    }

    /// <summary>
    /// StoreSummaryAsync stores the summary of the document in the database.
    /// </summary>
    /// <param name="documentId"></param>
    /// <param name="summary"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<int?> StoreSummaryAsync(string documentId, ConcurrentDictionary<string, object> summary, CancellationToken cancellationToken = default)
    {
        try 
        {
            var collection = _database.GetCollection<BsonDocument>("summaries");
            var document = new BsonDocument
            {
                { "_id", documentId },
                { "content", BsonDocument.Parse(JsonSerializer.Serialize(summary)) }
            };
            await collection.InsertOneAsync(document, cancellationToken: cancellationToken);
            return summary.Count; // MongoDB doesn't tell us how many went in
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save the summary of the document with id {Id} to the database.", documentId);
            return null;
        }
    }

    /// <summary>
    /// GetRelevantDocumentsAsync queries the database for the most relevant documents to the query vector.
    /// Biggest advantage of mongo - dot product aggregation.
    /// </summary>
    /// <param name="documentId"></param>
    /// <param name="queryVector"></param>
    /// <param name="topRelevantCount"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IEnumerable<ConcurrentDictionary<string, object>>> GetRelevantDocumentsAsync(string documentId, float[] queryVector, int topRelevantCount, CancellationToken cancellationToken = default)
    {
        var collection = _database.GetCollection<BsonDocument>("documents");
        var pipeline = new BsonDocument[]
        {
            new("$match", new BsonDocument("_id", documentId)),
            new("$addFields", 
                new BsonDocument("vectorScore", 
                new BsonDocument("$dotProduct", 
                new BsonArray { "$embedding", new BsonArray(queryVector) }))
            ),
            new("$sort", new BsonDocument("vectorScore", -1)),
            new("$limit", topRelevantCount),
            new("$project", new BsonDocument
            {
                { "content", 1 },
                { "_id", 0 }
            })
        };
        var results = await collection.Aggregate<BsonDocument>(pipeline, cancellationToken: cancellationToken).ToListAsync(cancellationToken);
        return results.Select(doc => JsonSerializer.Deserialize<ConcurrentDictionary<string, object>>(doc["content"].ToJson(), _serializerOptions)!);
    }

    /// <summary>
    /// Simple lookup on the summaries collection
    /// </summary>
    /// <param name="documentId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ConcurrentDictionary<string, object>> GetSummaryAsync(string documentId, CancellationToken cancellationToken = default)
    {
        var collection = _database.GetCollection<BsonDocument>("summaries");
        var filter = Builders<BsonDocument>.Filter.Eq("_id", documentId);
        var result = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        if (result == null) return new ConcurrentDictionary<string, object>();
        return JsonSerializer.Deserialize<ConcurrentDictionary<string, object>>(result["content"].ToJson(), _serializerOptions)!;
    }
}