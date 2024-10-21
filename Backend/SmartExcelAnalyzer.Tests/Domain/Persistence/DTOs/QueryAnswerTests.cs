using Domain.Persistence.DTOs;
using System.Collections.Concurrent;

namespace SmartExcelAnalyzer.Tests.Domain.Persistence.DTOs;

public class QueryAnswerTests
{
    [Fact]
    public void QueryAnswer_Properties_ShouldBeSettable()
    {

        var queryAnswer = new QueryAnswer
        {
            Answer = "Test Answer",
            Question = "Test Question",
            DocumentId = "Test Document ID"
        };
        var relevantRows = new ConcurrentBag<ConcurrentDictionary<string, object>>
        {
            new() { ["TestKey"] = "TestValue" }
        };
        queryAnswer.RelevantRows = relevantRows;

        Assert.Equal("Test Question", queryAnswer.Question);
        Assert.Equal("Test Document ID", queryAnswer.DocumentId);
        Assert.Equal("Test Answer", queryAnswer.Answer);
        Assert.Single(queryAnswer.RelevantRows);
        Assert.Equal("TestValue", queryAnswer.RelevantRows.First()["TestKey"]);
    }

    [Fact]
    public void QueryAnswer_DefaultValues_ShouldBeCorrect()
    {
        var queryAnswer = new QueryAnswer
        {
            Answer = "Test Answer"
        };

        Assert.Equal("", queryAnswer.Question);
        Assert.Equal("", queryAnswer.DocumentId);
        Assert.Equal("Test Answer", queryAnswer.Answer);
        Assert.Empty(queryAnswer.RelevantRows);
    }
}