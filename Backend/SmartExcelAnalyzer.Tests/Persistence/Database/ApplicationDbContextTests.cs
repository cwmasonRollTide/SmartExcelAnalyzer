using Microsoft.EntityFrameworkCore;
using Persistence.Database;
using Domain.Persistence;
using FluentAssertions;

namespace SmartExcelAnalyzer.Tests.Persistence.Database;

public class ApplicationDbContextTests
{
    [Fact]
    public void DbSet_Properties_AreCorrectlyDefined()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        using var context = new ApplicationDbContext(options);

        context.Summaries.Should().NotBeNull(); 
        context.Documents.Should().NotBeNull();
    }

    [Fact]
    public void OnModelCreating_ConfiguresDocumentEmbeddingCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        using var context = new ApplicationDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Document))!;
        var embeddingProperty = entityType!.FindProperty(nameof(Document.Embedding)!)!;

        embeddingProperty!.Should().NotBeNull();
        embeddingProperty!.IsNullable.Should().BeFalse();
        embeddingProperty!.GetValueConverter().Should().NotBeNull();
    }

    [Fact]
    public void EmbeddingConversion_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        using var context = new ApplicationDbContext(options);

        var document = new Document
        {
            Id = "1",
            Content = "Test content",
            Embedding = [1.0f, 2.0f, 3.0f]
        };

        context.Documents.Add(document);
        context.SaveChanges();

        var retrievedDocument = context.Documents.FirstOrDefault(d => d.Id == "1");

        retrievedDocument.Should().NotBeNull();
        document.Embedding.Should().BeEquivalentTo(retrievedDocument!.Embedding);
    }
}