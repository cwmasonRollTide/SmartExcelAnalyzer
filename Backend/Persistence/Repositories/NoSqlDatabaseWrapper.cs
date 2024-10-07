using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Persistence.Repositories;

public class NoSqlDatabaseWrapper(IMongoDatabase database, ILogger<NoSqlDatabaseWrapper> logger, ILLMRepository llmRepository) : IDatabaseWrapper
{
    private readonly IMongoDatabase _database = database;
    private readonly ILogger<NoSqlDatabaseWrapper> _logger = logger;
    private readonly ILLMRepository _llmRepository = llmRepository;
    private readonly JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<string> StoreVectorsAsync(ConcurrentBag<ConcurrentDictionary<string, object>> rows, CancellationToken cancellationToken = default)
    {
        var collection = _database.GetCollection<BsonDocument>("documents");
        var documentId = ObjectId.GenerateNewId().ToString();
        var documents = await Task.WhenAll(rows.Select(async row =>
        {
            var serializedRow = JsonSerializer.Serialize(row);
            var embedding = await _llmRepository.ComputeEmbedding(serializedRow, cancellationToken);
            return new BsonDocument
            {
                { "_id", documentId },
                { "content", BsonDocument.Parse(serializedRow) },
                { "embedding", new BsonArray(embedding) }
            };
        }));

        await collection.InsertManyAsync(documents, cancellationToken: cancellationToken);
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