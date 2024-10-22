using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Persistence.Repositories;

public class QdrantClientWrapper(QdrantClient client) : IQdrantClient
{
    private readonly QdrantClient _client = client;

    public Task<UpdateResult> UpsertAsync(
        string collectionName,
        IReadOnlyList<PointStruct> points,
        bool wait = true,
        WriteOrderingType? ordering = null,
        ShardKeySelector? shardKeySelector = null,
        CancellationToken cancellationToken = default)
    {
        return _client.UpsertAsync(collectionName, points, wait, ordering, shardKeySelector, cancellationToken);
    }

    public Task<IReadOnlyList<ScoredPoint>> SearchAsync(
        string collectionName,
        ReadOnlyMemory<float> vector,
        Filter? filter = null,
        SearchParams? searchParams = null,
        ulong limit = 10,
        ulong offset = 0,
        WithPayloadSelector? payloadSelector = null,
        WithVectorsSelector? vectorsSelector = null,
        float? scoreThreshold = null,
        string? vectorName = null,
        ReadConsistency? readConsistency = null,
        ShardKeySelector? shardKeySelector = null,
        ReadOnlyMemory<uint>? sparseIndices = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        return _client.SearchAsync(collectionName, vector, filter, searchParams, limit, offset, payloadSelector, vectorsSelector, scoreThreshold, vectorName, readConsistency, shardKeySelector, sparseIndices, timeout, cancellationToken);
    }
}
