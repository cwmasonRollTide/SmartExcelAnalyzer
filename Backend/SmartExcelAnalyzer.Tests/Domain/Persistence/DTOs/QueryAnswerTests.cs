using Domain.Persistence.DTOs;
using FluentAssertions;
using System.Collections.Concurrent;

namespace SmartExcelAnalyzer.Tests.Domain.Persistence.DTOs;

public class QueryAnswerTests
{
    [Fact]
    public void QueryAnswer_Properties_ShouldBeSettable()
    {
        var question = "Test Question";
        var documentId = "Test Document ID";
        var answer = "Test Answer";
        var relevantRows = new ConcurrentBag<ConcurrentDictionary<string, object>>
        {
            new() 
            { 
                ["TestKey"] = "TestValue" 
            }
        };

        var queryAnswer = new QueryAnswer
        {
            Answer = answer,
            Question = question,
            DocumentId = documentId,
            RelevantRows = relevantRows
        };

        queryAnswer.Answer.Should().Be(answer);
        queryAnswer.Question.Should().Be(question);
        queryAnswer.DocumentId.Should().Be(documentId);
        queryAnswer.RelevantRows.Should().BeEquivalentTo(relevantRows);
    }

    [Fact]
    public void QueryAnswer_DefaultValues_ShouldBeCorrect()
    {
        var queryAnswer = new QueryAnswer
        {
            Answer = "Test Answer"
        };

        queryAnswer.RelevantRows.Should().BeEmpty();
        queryAnswer.Question.Should().BeEmpty();
        queryAnswer.DocumentId.Should().BeEmpty();
        queryAnswer.Answer.Should().Be("Test Answer");
    }
}