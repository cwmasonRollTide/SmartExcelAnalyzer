using System.Collections.Concurrent;

namespace Persistence.Database;

public interface IDatabaseWrapper
{
    Task<ConcurrentDictionary<string, object>> GetSummaryAsync(
        string documentId, 
        CancellationToken cancellationToken = default
    );
    
    Task<int?> StoreSummaryAsync(
        string documentId, 
        ConcurrentDictionary<string, object> summary, 
        CancellationToken cancellationToken = default
    );

    Task<string?> StoreVectorsAsync(
        IEnumerable<ConcurrentDictionary<string, object>> rows, 
        string? docId = null, 
        CancellationToken cancellationToken = default
    );

    Task<IEnumerable<ConcurrentDictionary<string, object>>> GetRelevantDocumentsAsync(
        string documentId, 
        float[] queryVector, 
        int topRelevantCount, 
        CancellationToken cancellationToken = default
    );
}