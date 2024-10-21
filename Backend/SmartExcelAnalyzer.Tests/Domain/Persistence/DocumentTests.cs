using FluentAssertions;
using Domain.Persistence;

namespace SmartExcelAnalyzer.Tests.Domain.Persistence;

public class DocumentTests
{
    [Fact]
    public void Document_Properties_ShouldSetAndGetCorrectly()
    {
        var id = "doc123";
        var content = "This is a test document";
        float[] testVector = [0.1f, 0.2f, 0.3f];
        var document = new Document
        {
            Id = id,
            Content = content,
            Embedding = testVector
        };

        document.Id.Should().Be(id);
        document.Content.Should().Be(content);
        document.Embedding.Should().BeEquivalentTo(testVector);
    }

    [Fact]
    public void Document_Embedding_ShouldAllowEmptyArray()
    {
        var document = new Document
        {
            Id = "doc456",
            Content = "Another test document",
            Embedding = []
        };

        document.Embedding.Should().BeEmpty();
    }

    [Fact]
    public void Document_Embedding_ShouldAllowNullArray()
    {
        var document = new Document
        {
            Id = "doc789",
            Content = "Yet another test document",
            Embedding = null!
        };

        document.Embedding.Should().BeNull();
    }
}