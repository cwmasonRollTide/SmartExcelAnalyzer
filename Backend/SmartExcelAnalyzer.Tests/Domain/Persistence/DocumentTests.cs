using Domain.Persistence;

namespace SmartExcelAnalyzer.Tests.Domain.Persistence;

public class DocumentTests
{
    [Fact]
    public void Document_Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var document = new Document
        {
            Id = "doc123",
            Content = "This is a test document",
            Embedding = new float[] { 0.1f, 0.2f, 0.3f }
        };

        // Act & Assert
        Assert.Equal("doc123", document.Id);
        Assert.Equal("This is a test document", document.Content);
        Assert.Equal(new float[] { 0.1f, 0.2f, 0.3f }, document.Embedding);
    }

    [Fact]
    public void Document_Embedding_ShouldAllowEmptyArray()
    {
        // Arrange & Act
        var document = new Document
        {
            Id = "doc456",
            Content = "Another test document",
            Embedding = Array.Empty<float>()
        };

        // Assert
        Assert.Empty(document.Embedding);
    }

    [Fact]
    public void Document_Embedding_ShouldAllowNullArray()
    {
        // Arrange & Act
        var document = new Document
        {
            Id = "doc789",
            Content = "Yet another test document",
            Embedding = null
        };

        // Assert
        Assert.Null(document.Embedding);
    }
}