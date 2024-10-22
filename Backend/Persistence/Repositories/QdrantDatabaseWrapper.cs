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
    IQdrantClient client,
    IOptions<DatabaseOptions> options, 
    ILogger<QdrantDatabaseWrapper> logger
) : IDatabaseWrapper
{
    #region Dependencies
    private readonly IQdrantClient _client = client;
    private readonly ILogger<QdrantDatabaseWrapper> _logger = logger;
    private readonly JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true };
    #endregion

    #region Fields
    private readonly Vectors _dummyVector = new(new float[] { 0.0f });
    private int BatchSize => options.Value.SAVE_BATCH_SIZE;
    private string CollectionName => options.Value.CollectionName;
    private string SummaryCollectionName => options.Value.CollectionNameTwo;
    private int MaxDegreeOfParallelism => options.Value.MAX_CONNECTION_COUNT;

    private static byte[] RandomBytes => new byte[4];
    private static Random Random => new();
    private static byte[] NextRandomBytes
    {
        get
        {
            Random.NextBytes(RandomBytes);
            return RandomBytes;
        }
    } 
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
    public async Task<string?> StoreVectorsAsync(
        IEnumerable<ConcurrentDictionary<string, object>> rows,
        string? documentId = null,
        CancellationToken cancellationToken = default
    )
    {   
        documentId ??= NewDocumentId();
        var points = await CreateRowsInParallelAsync(rows, documentId, cancellationToken);
        if (points.Any()) await InsertRowsInBatchesAsync(points, cancellationToken);
        return documentId;
    }

    private static string NewDocumentId() => BitConverter.ToString(NextRandomBytes).Replace("-", "");

    /// <summary>
    /// StoreSummaryAsync stores the summary of the document in the database.
    /// Dummy vector - Qdrant requires a vector
    /// </summary>
    /// <param name="documentId"></param>
    /// <param name="summary"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Greater than 0 if success</returns>
    public async Task<int?> StoreSummaryAsync(
        string documentId, 
        ConcurrentDictionary<string, object> summary, 
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var summaryData = new PointStruct { Id = new PointId(), Vectors = _dummyVector };
            summaryData.Payload["is_summary"] = new Value { BoolValue = true };
            summaryData.Payload["document_id"] = new Value { StringValue = documentId };
            summaryData.Payload["content"] = new Value { StringValue = JsonSerializer.Serialize(summary, _serializerOptions) };
            await _client.UpsertAsync(
                points: [summaryData],
                collectionName: SummaryCollectionName,
                cancellationToken: cancellationToken
            );
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
            collectionName: CollectionName,
            vector: queryVector.AsMemory(),
            filter: MatchKeyword("document_id", documentId),
            limit: (ulong)topRelevantCount,
            cancellationToken: cancellationToken
        );
        return searchResult?
            .Select(point => point.Payload)
            .Select(point => point.TryGetValue("content", out var contentValue) 
                ? contentValue.StringValue 
                : null)
            .Select(content => content is not null 
                ? JsonSerializer.Deserialize<ConcurrentDictionary<string, object>>(content, _serializerOptions) 
                : null)
            .Where(content => content is not null) // Filter out null values
            .Select(content => content!) 
        ?? [];
    }

    /// Simple lookup for the summary
    /// </summary>
    /// <param name="documentId"></param>
    /// <param name="cancellationToken"></param>    
    /// <returns></returns>
    public async Task<ConcurrentDictionary<string, object>> GetSummaryAsync(string documentId, CancellationToken cancellationToken = default)
    {
        var searchResult = await _client.SearchAsync(
            collectionName: SummaryCollectionName,
            vector: new float[1],
            filter: MatchKeyword("document_id", documentId),
            limit: 1,
            cancellationToken: cancellationToken
        );
        if (searchResult is null || searchResult.Count is 0) return new ConcurrentDictionary<string, object>();

        var summary = searchResult[0];
        if (!summary!.Payload.TryGetValue("content", out Value? value)) return new ConcurrentDictionary<string, object>();

        return JsonSerializer.Deserialize<ConcurrentDictionary<string, object>>(value.StringValue, _serializerOptions)!;
    }
    #endregion

    #region Private Methods
    private async Task<IEnumerable<PointStruct>> CreateRowsInParallelAsync(
        IEnumerable<ConcurrentDictionary<string, object>> rows,
        string? documentId,
        CancellationToken cancellationToken = default
    )
    {
        var parallelOptions = CreateParallelOptions(cancellationToken);
        var points = new ConcurrentBag<PointStruct>();
        await Parallel.ForEachAsync(
            rows, 
            parallelOptions, 
            async (row, ct) =>
            {
                await Task.Yield();
                var point = CreateRow(row, documentId!);
                points.Add(point);
            }
        );
        return points;
    }

    private PointStruct CreateRow(ConcurrentDictionary<string, object> row, string? documentId)
    {
        var point = new PointStruct 
        { 
            Id = new PointId(), 
            Vectors = row.TryGetValue("embedding", out var embedding) ? (embedding as Vectors) ?? Array.Empty<float>() : Array.Empty<float>()
        };
        if (documentId is not null)
        {
            point.Payload.Add("document_id", new Value { StringValue = documentId.ToString() });
        }
        point.Payload.Add("content", new Value { StringValue = JsonSerializer.Serialize(row, _serializerOptions)});
        return point;
    }

    private async Task InsertRowsInBatchesAsync(IEnumerable<PointStruct> points, CancellationToken cancellationToken = default)
    {
        int totalInserted = 0;
        foreach (var batch in points.Chunk(BatchSize))
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

    private ParallelOptions CreateParallelOptions(CancellationToken cancellationToken = default) => 
        new()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = MaxDegreeOfParallelism
        };
    #endregion
}