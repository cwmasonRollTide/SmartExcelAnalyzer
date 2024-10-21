using Domain.Persistence.DTOs;
using System.Collections.Concurrent;

namespace SmartExcelAnalyzer.Tests.Domain.Persistence.DTOs;

public class QueryAnswerTests
{
    [Fact]
    public void QueryAnswer_Properties_ShouldBeSettable()
    {
        // Arrange
        var queryAnswer = new QueryAnswer
        {
            Answer = "Test Answer" // This is required
        };

        // Act
        queryAnswer.Question = "Test Question";
        queryAnswer.DocumentId = "Test Document ID";
        var relevantRows = new ConcurrentBag<ConcurrentDictionary<string, object>>();
        relevantRows.Add(new ConcurrentDictionary<string, object> { ["TestKey"] = "TestValue" });
        queryAnswer.RelevantRows = relevantRows;

        // Assert
        Assert.Equal("Test Question", queryAnswer.Question);
        Assert.Equal("Test Document ID", queryAnswer.DocumentId);
        Assert.Equal("Test Answer", queryAnswer.Answer);
        Assert.Single(queryAnswer.RelevantRows);
        Assert.Equal("TestValue", queryAnswer.RelevantRows.First()["TestKey"]);
    }

    [Fact]
    public void QueryAnswer_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var queryAnswer = new QueryAnswer
        {
            Answer = "Test Answer" // This is required
        };

        // Assert
        Assert.Equal("", queryAnswer.Question);
        Assert.Equal("", queryAnswer.DocumentId);
        Assert.Equal("Test Answer", queryAnswer.Answer);
        Assert.Empty(queryAnswer.RelevantRows);
    }
}