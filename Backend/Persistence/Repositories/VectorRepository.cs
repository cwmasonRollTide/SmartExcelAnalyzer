using System.Data;
using System.Text.Json;
using Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public interface IVectorDbRepository
{
    Task<string> SaveDocumentAsync(List<Dictionary<string, DataRow>> excelData, Dictionary<string, Summary> summary);
    Task<(List<Dictionary<string, DataRow>> RelevantRows, Dictionary<string, Summary> Summary)> QueryVectors(string documentId, float[] query);
}

public class VectorDbRepository(ApplicationDbContext context, ILLMRepository lLMRepository) : IVectorDbRepository
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILLMRepository _llmRepository = lLMRepository;

    public async Task<string> SaveDocumentAsync(List<Dictionary<string, DataRow>> excelData, Dictionary<string, Summary> summary)
    {
        string documentId = await StoreVectors(excelData);
        await StoreSummary(documentId, summary);
        return documentId;
    }

    private async Task<string> StoreVectors(List<Dictionary<string, DataRow>> rows)
    {
        var documentId = Guid.NewGuid().ToString();
        var documentTasks = rows.Select(row => GenerateDocument(documentId, row)).ToList();
        var documents = await Task.WhenAll(documentTasks);
        _context.Documents.AddRange(documents);
        await _context.SaveChangesAsync();
        return documentId;
    }

    private async Task StoreSummary(string documentId, Dictionary<string, Summary> summary)
    {
        _context.Summaries.Add(new Summary { Id = documentId, Content = JsonSerializer.Serialize(summary) });
        await _context.SaveChangesAsync();
    }

    public async Task<(List<Dictionary<string, DataRow>> RelevantRows, Dictionary<string, Summary> Summary)> QueryVectors(string documentId, float[] queryVector)
    {
        var serializerSettings = new JsonSerializerOptions();
        var relevantDocuments = await _context.Documents
            .Where(d => d.Id == documentId)
            .OrderByDescending(d => CalculateSimilarity(d.Embedding, queryVector!))
            .Take(10)
            .ToListAsync();
        var relevantRows = relevantDocuments
            .Select(d => JsonSerializer.Deserialize<Dictionary<string, DataRow>>(d.Content)!)
            .ToList();
        var summary = await _context.Summaries
            .Where(s => s.Id == documentId)
            .Select(s => JsonSerializer.Deserialize<Dictionary<string, Summary>>(s.Content, serializerSettings))
            .FirstOrDefaultAsync() ?? [];
        return (RelevantRows: relevantRows, Summary: summary);
    }

    private async Task<Document> GenerateDocument(string documentId, Dictionary<string, DataRow> row) 
    {
        var serializedDoc = JsonSerializer.Serialize(row);
        var embedding = await _llmRepository.ComputeEmbedding(documentId, serializedDoc);
        return new()
        {
            Id = documentId,
            Content = serializedDoc,
            Embedding = embedding!
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
    private static float CalculateSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) throw new ArgumentException("Vectors must be of the same length");
        return VectorMath.DotProduct(a, b) / (VectorMath.Magnitude(a) * VectorMath.Magnitude(b));
    }

    /// <summary>
    /// Helper class for vector math operations.
    /// Vector math is at the core of many machine learning algorithms,
    /// and it is essential for tasks such as calculating distances,
    /// angles, and projections between vectors.
    /// </summary>
    private static class VectorMath
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
