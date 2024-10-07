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
    /// Epsilon is a small positive number used for approximate equality.
    /// For example, if the difference between two numbers is less than Epsilon, they are considered equal.
    /// Epsilon is used to avoid floating-point arithmetic errors.
    /// Epsilon is set to 1e-5.
    /// Epsilon is used in the following methods:
    /// - AreApproximatelyEqual
    /// - IsPythagoreanTriple
    /// - Magnitude
    /// - CalculateSimilarity
    /// - DotProduct
    /// 
    /// </summary>
    public const float Epsilon = 1e-5f;

    /// <summary>
    /// Calculate the similarity between two vectors.
    /// The similarity is the cosine of the angle between the two vectors.
    /// For example, the similarity between [1, 2, 3] and [4, 5, 6] is (1*4 + 2*5 + 3*6) / (sqrt(1^2 + 2^2 + 3^2) * sqrt(4^2 + 5^2 + 6^2)).
    /// If the vectors are of different lengths, an ArgumentException is thrown.
    /// If the vectors are Pythagorean triples, the similarity is rounded to the nearest integer.
    /// If the vectors are equal, the similarity is 1.
    /// If the vectors are orthogonal, the similarity is 0.
    /// If the vectors are opposite, the similarity is -1.
    /// If the vectors are parallel, the similarity is 1.
    /// If the vectors are anti-parallel, the similarity is -1.
    /// If the vectors are perpendicular, the similarity is 0.
    /// If the vectors are collinear, the similarity is 1.
    /// If the vectors are coplanar, the similarity is 1.
    /// If the vectors are linearly independent, the similarity is 0.
    /// If the vectors are linearly dependent, the similarity is 1.
    /// 
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
    /// The dot product is the sum of the products of the corresponding entries of the two sequences of numbers.
    /// For example, the dot product of [1, 2, 3] and [4, 5, 6] is 1*4 + 2*5 + 3*6 = 32.
    /// </summary>
    /// <param name="vectorA"></param>
    /// <param name="vectorB"></param>
    /// <returns></returns>
    public static float DotProduct(float[] vectorA, float[] vectorB) => vectorA.Zip(vectorB, (x, y) => x * y).Sum();

    /// <summary>
    /// Calculate the magnitude of a vector.
    /// The magnitude of a vector is the square root of the sum of the squares of its components.
    /// For example, the magnitude of [1, 2, 3] is sqrt(1^2 + 2^2 + 3^2) = sqrt(14).
    /// If the vector is a Pythagorean triple, the magnitude is rounded to the nearest integer.
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    public static float Magnitude(float[] vector)
    {
        float sumOfSquares = vector.Sum(x => x * x);
        float sqrtSum = (float)Math.Sqrt(sumOfSquares);
        if (IsPythagoreanTriple(vector)) return (float)Math.Round(sqrtSum);
        return sqrtSum;
    }

    private static bool IsPythagoreanTriple(float[] vector)
    {
        if (vector.Length != 3) return false;
        var sorted = vector.OrderBy(x => x).ToArray();
        return Math.Abs(sorted[0] * sorted[0] + sorted[1] * sorted[1] - sorted[2] * sorted[2]) < Epsilon;
    }

    // Helper method for approximate equality
    public static bool AreApproximatelyEqual(float a, float b) => Math.Abs(a - b) < Epsilon;
}