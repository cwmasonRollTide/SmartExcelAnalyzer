using Microsoft.EntityFrameworkCore;
using Persistence.Database;
using Domain.Persistence;

namespace SmartExcelAnalyzer.Tests.Persistence.Database;

public class ApplicationDbContextTests
{
    [Fact]
    public void DbSet_Properties_AreCorrectlyDefined()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        // Act
        using var context = new ApplicationDbContext(options);

        // Assert
        Assert.NotNull(context.Summaries);
        Assert.NotNull(context.Documents);
    }

    [Fact]
    public void OnModelCreating_ConfiguresDocumentEmbeddingCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        // Act
        using var context = new ApplicationDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Document));
        var embeddingProperty = entityType.FindProperty(nameof(Document.Embedding));

        // Assert
        Assert.NotNull(embeddingProperty);
        Assert.False(embeddingProperty.IsNullable);
        Assert.NotNull(embeddingProperty.GetValueConverter());
    }

    [Fact]
    public void EmbeddingConversion_WorksCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        using var context = new ApplicationDbContext(options);

        var document = new Document
        {
            Id = "1",
            Content = "Test content",
            Embedding = new float[] { 1.0f, 2.0f, 3.0f }
        };

        // Act
        context.Documents.Add(document);
        context.SaveChanges();

        var retrievedDocument = context.Documents.FirstOrDefault(d => d.Id == "1");

        // Assert
        Assert.NotNull(retrievedDocument);
        Assert.Equal(document.Embedding, retrievedDocument.Embedding);
    }
}