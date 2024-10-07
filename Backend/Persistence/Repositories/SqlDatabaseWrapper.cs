using System.Collections.Concurrent;
using System.Text.Json;
using Domain.Persistence;
using Domain.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.Database;

namespace Persistence.Repositories;

public class SqlDatabaseWrapper(ApplicationDbContext context, ILogger<SqlDatabaseWrapper> logger, ILLMRepository llmRepository) : IDatabaseWrapper
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<SqlDatabaseWrapper> _logger = logger;
    private readonly ILLMRepository _llmRepository = llmRepository;
    private readonly JsonSerializerOptions _serializerSettings = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<string> StoreVectorsAsync(ConcurrentBag<ConcurrentDictionary<string, object>> rows, CancellationToken cancellationToken = default)
    {
        var documentId = Guid.NewGuid().ToString();
        var documents = await Task.WhenAll(rows.Select(row => GenerateDocument(documentId, row, cancellationToken)));
        _context.Documents.AddRange(documents);
        if (await _context.SaveChangesAsync(cancellationToken) > -1) return documentId;
        return null!;
    }

    public async Task<IEnumerable<ConcurrentDictionary<string, object>>> GetRelevantDocumentsAsync(string documentId, float[] queryVector, int topRelevantCount, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Where(d => d.Id == documentId)
            .OrderByDescending(d => VectorMath.CalculateSimilarity(d.Embedding, queryVector))
            .Take(topRelevantCount)
            .Select(d => JsonSerializer.Deserialize<ConcurrentDictionary<string, object>>(d.Content, _serializerSettings)!)
            .ToListAsync(cancellationToken);
    }

    public async Task<ConcurrentDictionary<string, object>> GetSummaryAsync(string documentId, CancellationToken cancellationToken = default)
    {
        var summary = await _context.Summaries
            .Where(s => s.Id == documentId)
            .Select(s => s.Content)
            .FirstOrDefaultAsync(cancellationToken);
        return JsonSerializer.Deserialize<ConcurrentDictionary<string, object>>(summary!, _serializerSettings)!;
    }

    private async Task<Document> GenerateDocument(string documentId, ConcurrentDictionary<string, object> row, CancellationToken cancellationToken = default)
    {
        var serializedData = JsonSerializer.Serialize(row);
        return new Document
        {
            Id = documentId,
            Content = serializedData,
            Embedding = await _llmRepository.ComputeEmbedding(serializedData, cancellationToken) ?? []
        };
    }

    public async Task<int?> StoreSummaryAsync(string documentId, ConcurrentDictionary<string, object> summary, CancellationToken cancellationToken)
    {
        _context.Summaries.Add(new Summary { Id = documentId, Content = JsonSerializer.Serialize(summary) });
        return await _context.SaveChangesAsync(cancellationToken);
    }
}