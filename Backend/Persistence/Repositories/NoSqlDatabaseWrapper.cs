using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Persistence.Database;

namespace Persistence.Repositories;

public class NoSqlDatabaseWrapper(IMongoDatabase database, ILogger<NoSqlDatabaseWrapper> logger) : IDatabaseWrapper
{
    private readonly IMongoDatabase _database = database;
    private readonly ILogger<NoSqlDatabaseWrapper> _logger = logger;
    private readonly JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<string> StoreVectorsAsync(ConcurrentBag<ConcurrentDictionary<string, object>> rows, string? docId = null, CancellationToken cancellationToken = default)
    {
        var collection = _database.GetCollection<BsonDocument>("documents");
        var documentId = docId ?? ObjectId.GenerateNewId().ToString();
        const int batchSize = 1000; // Consider making this configurable
        const int maxConcurrentTasks = 4; // Limit concurrent tasks, adjust based on your system's capabilities

        var batches = rows
            .Select((row, index) => new { Row = row, Index = index })
            .GroupBy(x => x.Index / batchSize)
            .Select(g => g.Select(x => x.Row).ToList())
            .ToList();

        using var semaphore = new SemaphoreSlim(maxConcurrentTasks);
        var tasks = batches.Select(async batch =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var batchDocuments = batch.Select(row =>
                {
                    var embeddingArray = (row["embedding"] as float[]) ?? [];
                    return new BsonDocument
                    {
                        { "_id", ObjectId.GenerateNewId() }, // Generate unique ID for each document
                        { "documentId", documentId }, // Add a field to group documents
                        { "content", BsonDocument.Parse(JsonSerializer.Serialize(row)) },
                        { "embedding", new BsonArray(embeddingArray) }
                    };
                }).ToList();

                var retryCount = 0;
                while (retryCount < 3) // Retry up to 3 times
                {
                    try
                    {
                        await collection.InsertManyAsync(batchDocuments, cancellationToken: cancellationToken);
                        break;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogWarning(ex, "Failed to insert batch. Retry attempt: {RetryCount}", retryCount + 1);
                        retryCount++;
                        if (retryCount == 3) throw; // Rethrow if all retries failed
                        await Task.Delay(1000 * retryCount, cancellationToken); // Exponential backoff
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return documentId;
    }

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

    public async Task<IEnumerable<ConcurrentDictionary<string, object>>> GetRelevantDocumentsAsync(string documentId, float[] queryVector, int topRelevantCount, CancellationToken cancellationToken = default)
    {
        var collection = _database.GetCollection<BsonDocument>("documents");
        var pipeline = new BsonDocument[]
        {
            new("$match", new BsonDocument("_id", documentId)),
            new("$addFields", 
                new BsonDocument("vectorScore", 
                    new BsonDocument("$dotProduct", new BsonArray { "$embedding", new BsonArray(queryVector) }))),
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

    public async Task<ConcurrentDictionary<string, object>> GetSummaryAsync(string documentId, CancellationToken cancellationToken = default)
    {
        var collection = _database.GetCollection<BsonDocument>("summaries");
        var filter = Builders<BsonDocument>.Filter.Eq("_id", documentId);
        var result = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        if (result == null) return new ConcurrentDictionary<string, object>();
        return JsonSerializer.Deserialize<ConcurrentDictionary<string, object>>(result["content"].ToJson(), _serializerOptions)!;
    }
}