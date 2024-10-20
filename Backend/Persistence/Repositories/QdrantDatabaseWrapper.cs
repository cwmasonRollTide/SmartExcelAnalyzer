using Qdrant.Client;
using System.Text.Json;
using Qdrant.Client.Grpc;
using Persistence.Database;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using Domain.Persistence.Configuration;
using static Qdrant.Client.Grpc.Conditions;

namespace Persistence.Repositories;

public class QdrantDatabaseWrapper(
    QdrantClient client,
    IOptions<DatabaseOptions> options, 
    ILogger<QdrantDatabaseWrapper> logger
) : IDatabaseWrapper
{
    #region Dependencies
    private readonly QdrantClient _client = client;
    private readonly ILogger<QdrantDatabaseWrapper> _logger = logger;
    private readonly JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true };
    #endregion

    #region Fields
    private int _batchSize => options.Value.SAVE_BATCH_SIZE;
    private string CollectionName => options.Value.CollectionName;
    private string SummaryCollectionName => options.Value.CollectionNameTwo;
    private int MaxDegreeOfParallelism => options.Value.MAX_CONNECTION_COUNT;
    #endregion

    #region Public Methods
    /// <summary>
    /// StoreVectorsAsync stores the vectors in the database.
    /// Creates the rows in parallel and inserts them in batches.
    /// </summary>
    /// <param name="rows"></param>
    /// <param name="docId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string> StoreVectorsAsync(
        IEnumerable<ConcurrentDictionary<string, object>> rows,
        string? docId = default,
        CancellationToken cancellationToken = default)
    {
        var documentId = docId ?? Guid.NewGuid().ToString();
        var points = await CreateRowsInParallelAsync(rows, documentId, cancellationToken);
        await InsertRowsInBatchesAsync(points, cancellationToken);
        return documentId;
    }

    /// <summary>
    /// StoreSummaryAsync stores the summary of the document in the database.
    /// Dummy vector - Qdrant requires a vector
    /// </summary>
    /// <param name="documentId"></param>
    /// <param name="summary"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<int?> StoreSummaryAsync(
        string documentId, 
        ConcurrentDictionary<string, object> summary, 
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var point = new PointStruct
            {
                Id = new PointId(),
                Vectors = new float[1] 
            };
            point.Payload["is_summary"] = new Value { BoolValue = true };
            point.Payload["document_id"] = new Value { StringValue = documentId };
            point.Payload["content"] = new Value { StringValue = JsonSerializer.Serialize(summary, _serializerOptions) };
            await _client.UpsertAsync(collectionName: SummaryCollectionName, points: [point], cancellationToken: cancellationToken);
            return summary.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save the summary of the document with id {Id} to the database.", documentId);
            return null;
        }
    }

    /// <summary>
    /// GetRelevantDocumentsAsync queries the database for the most relevant documents to the query vector.
    /// </summary>
    /// <param name="documentId"></param>
    /// <param name="queryVector"></param>
    /// <param name="topRelevantCount"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IEnumerable<ConcurrentDictionary<string, object>>> GetRelevantDocumentsAsync(
        string documentId, 
        float[] queryVector, 
        int topRelevantCount, 
        CancellationToken cancellationToken = default
    )
    {
        var searchResult = await _client.SearchAsync(
            vector: queryVector,
            limit: (uint)topRelevantCount,
            collectionName: CollectionName,
            filter: MatchKeyword("document_id", documentId),
            cancellationToken: cancellationToken
        );
        return searchResult.Select(point => JsonSerializer.Deserialize<ConcurrentDictionary<string, object>>(point.Payload["content"].StringValue, _serializerOptions)!);
    }

    /// <summary>
    /// Simple lookup for the summary
    /// </summary>
    /// <param name="documentId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ConcurrentDictionary<string, object>> GetSummaryAsync(string documentId, CancellationToken cancellationToken = default)
    {
        var searchResult = await _client.SearchAsync(
            limit: 1,
            vector: new float[1],
            collectionName: SummaryCollectionName,
            filter: MatchKeyword("document_id", documentId),
            cancellationToken: cancellationToken
        );
        var summary = searchResult.FirstOrDefault();
        if (summary == null || !summary.Payload.TryGetValue("content", out Value? value))
        {
            return new ConcurrentDictionary<string, object>();
        }
        return JsonSerializer.Deserialize<ConcurrentDictionary<string, object>>(value.StringValue, _serializerOptions)!;
    }
    #endregion

    #region Private Methods
    private async Task<IEnumerable<PointStruct>> CreateRowsInParallelAsync(
        IEnumerable<ConcurrentDictionary<string, object>> rows,
        string documentId,
        CancellationToken cancellationToken
    )
    {
        var points = new ConcurrentBag<PointStruct>();
        await Parallel.ForEachAsync(rows, CreateParallelOptions(cancellationToken), 
            (row, ct) =>
            {
                var point = CreateRow(row, documentId);
                points.Add(point);
                return ValueTask.CompletedTask;
            }
        );
        return points;
    }

    private PointStruct CreateRow(ConcurrentDictionary<string, object> row, string documentId)
    {
        var point = new PointStruct { Id = new PointId(), Vectors = row["embedding"] as float[] ?? [] };
        point.Payload.Add("document_id", new Value { StringValue = documentId });
        point.Payload.Add("content", new Value { StringValue = JsonSerializer.Serialize(row, _serializerOptions)});
        return point;
    }

    private async Task InsertRowsInBatchesAsync(IEnumerable<PointStruct> points, CancellationToken cancellationToken)
    {
        int totalInserted = 0;
        foreach (var batch in points.Chunk(_batchSize))
        {
            try
            {
                await _client.UpsertAsync(
                    points: batch,
                    collectionName: CollectionName, 
                    cancellationToken: cancellationToken
                );
                totalInserted += batch.Length;
                _logger.LogInformation("Successfully inserted batch of {BatchSize} vectors. Progress: {Progress}", batch.Length, totalInserted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert batch starting at index {StartIndex}", totalInserted);
                throw;
            }
        }
    }

    private ParallelOptions CreateParallelOptions(CancellationToken cancellationToken) => 
        new()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = MaxDegreeOfParallelism
        };
    #endregion
}