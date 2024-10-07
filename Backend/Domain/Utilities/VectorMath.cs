namespace Domain.Utilities;

/// <summary>
/// Helper class for vector math operations.
/// Vector math enables machine learning 
/// and it is essential for tasks such as calculating distances,
/// angles, and projections between vectors.
/// </summary>
public static class VectorMath
{
    /// <summary>
    /// Calculate the similarity between two vectors.
    /// The similarity is a scalar value that represents the cosine of the angle between two vectors.
    /// </summary>
    /// <param name="vectorA"></param>
    /// <param name="vectorB"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static float CalculateSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length) throw new ArgumentException("Vectors must be of the same length");
        return DotProduct(vectorA, vectorB) / (Magnitude(vectorA) * Magnitude(vectorB));
    }

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
    /// 
    /// I was today years old when I learned about Zip
    /// </summary>
    /// <param name="vectorA">The first vector, represented as an array of floats.</param>
    /// <param name="vectorB">The second vector, represented as an array of floats.</param>
    /// <returns>The dot product of the two vectors as a float.</returns>
    public static float DotProduct(float[] vectorA, float[] vectorB) => vectorA.Zip(vectorB, (x, y) => x * y).Sum();

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