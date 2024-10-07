using FluentAssertions;
using Domain.Utilities;

namespace SmartExcelAnalyzer.Tests.Domain;

public class VectorMathTests
{
    [Theory]
    [InlineData(new float[] { 1, 0, 0 }, new float[] { 1, 0, 0 }, 1f, "Equal vectors")]
    [InlineData(new float[] { 1, 0, 0 }, new float[] { 0, 1, 0 }, 0f, "Orthogonal vectors")]
    [InlineData(new float[] { 1, 0, 0 }, new float[] { -1, 0, 0 }, -1f, "Opposite vectors")]
    [InlineData(new float[] { 1, 2, 3 }, new float[] { 2, 4, 6 }, 1f, "Parallel vectors")]
    [InlineData(new float[] { 1, 2, 3 }, new float[] { -2, -4, -6 }, -1f, "Anti-parallel vectors")]
    [InlineData(new float[] { 1, 0, 0 }, new float[] { 0, 1, 0 }, 0f, "Perpendicular vectors")]
    [InlineData(new float[] { 1, 2, 3 }, new float[] { 2, 4, 6 }, 1f, "Collinear vectors")]
    [InlineData(new float[] { 1, 0, 0 }, new float[] { 0, 1, 0 }, 0f, "Coplanar vectors (in this case, also perpendicular)")]
    [InlineData(new float[] { 1, 0, 0 }, new float[] { 0, 1, 0 }, 0f, "Linearly independent vectors")]
    [InlineData(new float[] { 1, 2, 3 }, new float[] { 2, 4, 6 }, 1f, "Linearly dependent vectors")]
    [InlineData(new float[] { 3, 4, 0 }, new float[] { 0, 0, 5 }, 0f, "Pythagorean triple (3,4,5) - orthogonal case")]
    public void CalculateSimilarity_VariousScenarios_ReturnsExpectedResult(float[] vectorA, float[] vectorB, float expected, string scenario)
    {
        VectorMath.CalculateSimilarity(vectorA, vectorB).Should().BeApproximately(expected, VectorMath.Epsilon, $"Scenario: {scenario}");
    }

    [Fact]
    public void CalculateSimilarity_DifferentLengths_ThrowsArgumentException()
    {
        var vectorA = new float[] { 1, 2, 3 };
        var vectorB = new float[] { 1, 2 };

        Action act = () => VectorMath.CalculateSimilarity(vectorA, vectorB);

        act
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("Vectors must be of the same length");
    }

    [Fact]
    public void CalculateSimilarity_OrthogonalVectors_ReturnsZero()
    {
        var vectorA = new float[] { 1, 0, 0 };
        var vectorB = new float[] { 0, 1, 0 };
        VectorMath.CalculateSimilarity(vectorA, vectorB).Should().Be(0);
    }

    [Fact]
    public void CalculateSimilarity_DifferentLengthVectors_ThrowsArgumentException()
    {
        var vectorA = new float[] { 1, 2, 3 };
        var vectorB = new float[] { 1, 2 };
        Action action = () => VectorMath.CalculateSimilarity(vectorA, vectorB);
        action.Should().Throw<ArgumentException>().WithMessage("Vectors must be of the same length");
    }

    [Theory]
    [InlineData(new float[] { -1, -2, -3 }, new float[] { 1, 2, 3 }, -1f)]
    [InlineData(new float[] { 1, 2, 3 }, new float[] { 4, 5, 6 }, 0.9746318f)]
    public void CalculateSimilarity_VariousVectors_ReturnsExpectedResult(float[] vectorA, float[] vectorB, float expected)
    {
        VectorMath.CalculateSimilarity(vectorA, vectorB).Should().BeApproximately(expected, 0.0001f);
    }

    [Fact]
    public void DotProduct_SameVectors_ReturnsSquaredMagnitude()
    {
        var vector = new float[] { 1, 2, 3 };
        var result = VectorMath.DotProduct(vector, vector);
        result.Should().Be(14); // 1*1 + 2*2 + 3*3 = 14
    }

    [Fact]
    public void DotProduct_OrthogonalVectors_ReturnsZero()
    {
        var vectorA = new float[] { 1, 0, 0 };
        var vectorB = new float[] { 0, 1, 0 };
        VectorMath.DotProduct(vectorA, vectorB).Should().Be(0);
    }

    [Theory]
    [InlineData(new float[] { 1, 2, 3 }, new float[] { 4, 5, 6 }, 32)]
    [InlineData(new float[] { -1, -2, -3 }, new float[] { 1, 2, 3 }, -14)]
    public void DotProduct_VariousVectors_ReturnsExpectedResult(float[] vectorA, float[] vectorB, float expected)
    {
        VectorMath.DotProduct(vectorA, vectorB).Should().Be(expected);
    }

    [Fact]
    public void Magnitude_ZeroVector_ReturnsZero()
    {
        var vector = new float[] { 0, 0, 0 };
        VectorMath.Magnitude(vector).Should().Be(0);
    }

    [Fact]
    public void Magnitude_UnitVector_ReturnsOne()
    {
        var vector = new float[] { 1, 0, 0 };
        VectorMath.Magnitude(vector).Should().Be(1);
    }

    [Theory]
    [InlineData(new float[] { 3, 4 }, 5)]
    [InlineData(new float[] { 1, 1, 1 }, 1.7320508f)]
    public void Magnitude_VariousVectors_ReturnsExpectedResult(float[] vector, float expected)
    {
        VectorMath.Magnitude(vector).Should().BeApproximately(expected, 0.0001f);
    }

    [Theory]
    [InlineData(new float[] { 2, 3, 4 }, new float[] { 5, 6, 7 }, 56)]
    [InlineData(new float[] { 1, 1, 1, 1 }, new float[] { 2, 3, 4, 5 }, 14)]
    [InlineData(new float[] { 10, 20, 30 }, new float[] { 1, 2, 3 }, 140)]
    [InlineData(new float[] { 5, 5, 5, 5 }, new float[] { 1, 1, 1, 1 }, 20)]
    [InlineData(new float[] { 7, 8, 9 }, new float[] { 2, 2, 2 }, 48)]
    [InlineData(new float[] { 1, 2, 3, 4, 5 }, new float[] { 5, 4, 3, 2, 1 }, 35)]
    [InlineData(new float[] { 6, 7, 8, 9 }, new float[] { 1, 2, 3, 4 }, 80)]
    [InlineData(new float[] { 11, 12, 13 }, new float[] { 1, 1, 1 }, 36)]
    [InlineData(new float[] { 2, 4, 6, 8 }, new float[] { 1, 3, 5, 7 }, 100)]
    [InlineData(new float[] { 15, 16, 17, 18 }, new float[] { 1, 2, 3, 4 }, 170)]
    public void DotProduct_AdditionalVectors_ReturnsExpectedResult(float[] vectorA, float[] vectorB, float expected)
    {
        VectorMath.DotProduct(vectorA, vectorB).Should().Be(expected);
    }

    [Theory]
    [InlineData(new float[] { 5, 12 }, 13)]
    [InlineData(new float[] { 2, 3, 6 }, 7)]
    [InlineData(new float[] { 8, 15 }, 17)]
    [InlineData(new float[] { 7, 24, 25 }, 35)]
    [InlineData(new float[] { 5, 5, 5, 5 }, 10)]
    [InlineData(new float[] { 12, 16, 21 }, 29)]
    [InlineData(new float[] { 9, 12, 20 }, 25)]
    [InlineData(new float[] { 4, 4, 7 }, 9)]
    [InlineData(new float[] { 8, 9, 12 }, 17)]
    [InlineData(new float[] { 6, 8, 10 }, 14)]
    public void Magnitude_AdditionalVectors_ReturnsExpectedResult(float[] vector, float expected)
        {
            VectorMath.Magnitude(vector).Should().BeApproximately(expected, VectorMath.Epsilon);
        }

    [Fact]
    public void CalculateSimilarity_SameVectors_ReturnsOne()
    {
        var vector = new float[] { 1, 2, 3 };
        VectorMath.CalculateSimilarity(vector, vector).Should().BeApproximately(1f, VectorMath.Epsilon);
    }
}