
using System.Text.Json;
using Domain.Persistence;
using Domain.Persistence.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public interface IVectorDbRepository
{
    Task<string> SaveDocumentAsync(VectorResponse vectorSpreadsheetData, CancellationToken cancellationToken);
    Task<VectorResponse> QueryVectorData(string documentId, float[] queryVector, int topRelevantCount = 10, CancellationToken cancellationToken = default);
}

public class VectorDbRepository(
    ILLMRepository lLMRepository,
    ApplicationDbContext context
) : IVectorDbRepository
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILLMRepository _llmRepository = lLMRepository;
    private readonly JsonSerializerOptions _serializerSettings = new();

    /// <summary>
    /// Save the document to the database.
    /// The document is represented as a list of rows, where each row is a dictionary of column names and values.
    /// The document is stored in the database as a list of vectors, where each vector is the embedding of a row.
    /// The summary is stored in the database as a dictionary of summary statistics.
    /// </summary>
    /// <param name="vectorSpreadsheetData">RelevantRows, and Summary</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string> SaveDocumentAsync(
        VectorResponse vectorSpreadsheetData, 
        CancellationToken cancellationToken = default
    )
    {
        var documentId = await StoreVectors(vectorSpreadsheetData.RelevantRows);
        var summarySuccess = await StoreSummary(documentId, vectorSpreadsheetData.Summary);
        if (!summarySuccess.HasValue) 
        {
            // Rollback the document if the summary was not saved
            _context.Documents.RemoveRange(_context.Documents.Where(d => d.Id == documentId));
            await _context.SaveChangesAsync(cancellationToken);
            return null!;
        }
        return documentId;// Succeeded, return an identifier to be able to get the document later
    }

    public async Task<VectorResponse> QueryVectorData(
        string documentId, 
        float[] queryVector, 
        int topRelevantCount = 10,
        CancellationToken cancellationToken = default
    )
    {
        var relevantDocuments = await _context.Documents
            .Where(d => d.Id == documentId)
            .OrderByDescending(d => CalculateSimilarity(d.Embedding, queryVector!))
            .Take(topRelevantCount)
            .ToListAsync(cancellationToken);
        var relevantRows = relevantDocuments
            .Select(d => JsonSerializer.Deserialize<Dictionary<string, object>>(d.Content, _serializerSettings)!)
            .ToList();
        var summary = await _context.Summaries
            .Where(s => s.Id == documentId)
            .Select(s => JsonSerializer.Deserialize<Dictionary<string, object>>(s.Content, _serializerSettings))
            .FirstOrDefaultAsync(cancellationToken) ?? [];
        return new()
        {
            Summary = summary,
            RelevantRows = relevantRows
        };
    }

    private async Task<string> StoreVectors(
        List<Dictionary<string, object>> rows, 
        CancellationToken cancellationToken = default
    )
    {
        var documentId = Guid.NewGuid().ToString();
        var documentTasks = rows.Select(row => GenerateDocument(documentId, row, cancellationToken)).ToList();
        var documents = await Task.WhenAll(documentTasks);
        _context.Documents.AddRange(documents);
        var result = await _context.SaveChangesAsync(cancellationToken);
        if (result <= 0) return null!;
        return documentId;
    }

    private async Task<int?> StoreSummary(
        string documentId, 
        Dictionary<string, object> summary, 
        CancellationToken cancellationToken = default
    )
    {
        _context.Summaries.Add(new Summary { Id = documentId, Content = JsonSerializer.Serialize(summary) });
        return await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Document> GenerateDocument(
        string documentId, 
        Dictionary<string, object> row, 
        CancellationToken cancellationToken = default
    ) 
    {
        var serializedData = JsonSerializer.Serialize(row);
        var embedding = await _llmRepository.ComputeEmbedding(documentId, serializedData, cancellationToken);
        return new()
        {
            Id = documentId,
            Embedding = embedding!,
            Content = serializedData
        };
    }

    /// <summary>
    /// Calculate the similarity between two vectors.
    /// The similarity is a scalar value that represents the cosine of the angle between two vectors.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static float CalculateSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) throw new ArgumentException("Vectors must be of the same length");
        return VectorMath.DotProduct(a, b) / (VectorMath.Magnitude(a) * VectorMath.Magnitude(b));
    }

    /// <summary>
    /// Helper class for vector math operations.
    /// Vector math enables machine learning 
    /// and it is essential for tasks such as calculating distances,
    /// angles, and projections between vectors.
    /// </summary>
    public static class VectorMath
    {
        /// <summary>
        /// Calculate the dot product of two vectors.
        /// The dot product is a scalar value that is the result of the sum of the products 
        /// of the corresponding entries of the two sequences of numbers.
        /// 
        /// Mathematically, for two vectors a and b, each with n elements:
        /// DotProduct(a, b) = a[0]*b[0] + a[1]*b[1] + a[2]*b[2] + ... + a[n-1]*b[n-1]
        /// 
        /// This operation is often used in various fields such as physics, engineering, 
        /// and computer graphics to determine the angle between two vectors or to project 
        /// one vector onto another.
        /// </summary>
        /// <param name="a">The first vector, represented as an array of floats.</param>
        /// <param name="b">The second vector, represented as an array of floats.</param>
        /// <returns>The dot product of the two vectors as a float.</returns>
        public static float DotProduct(float[] a, float[] b) => a.Zip(b, (x, y) => x * y).Sum(); // I was today years old when I learned about Zip

        /// <summary>
        /// Calculate the magnitude of a vector.
        /// Magnitude == Euclidean norm == length, 
        /// Value that represents the distance of the vector from the origin 
        /// It is calculated as the square root of the sum of the 
        /// squares of its components.
        /// 
        /// Mathematically, for a vector v with n elements:
        /// Magnitude(v) = sqrt(v[0]^2 + v[1]^2 + v[2]^2 + ... + v[n-1]^2)
        /// 
        /// This operation is often used in various fields such as physics, engineering, 
        /// and computer graphics to determine the length of a vector, which can be useful 
        /// for normalizing vectors, calculating distances, and other vector operations.
        /// </summary>
        /// <param name="vector">The vector, represented as an array of floats.</param>
        /// <returns>The magnitude of the vector as a float.</returns>
        public static float Magnitude(float[] vector) => (float)Math.Sqrt(vector.Sum(x => x * x));
    }
}
