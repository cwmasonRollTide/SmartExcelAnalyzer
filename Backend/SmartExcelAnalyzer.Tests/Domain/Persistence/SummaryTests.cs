using FluentAssertions;
using Domain.Persistence;

namespace SmartExcelAnalyzer.Tests.Domain.Persistence;

public class SummaryTests
{
    [Fact]
    public void Summary_Properties_ShouldSetAndGetCorrectly()
    {
        var summary = new Summary
        {
            Id = "sum123",
            Content = "This is a test summary"
        };

        summary.Id.Should().Be("sum123");
        summary.Content.Should().Be("This is a test summary");
    }

    [Fact]
    public void Summary_Properties_ShouldAllowEmptyStrings()
    {
        var summary = new Summary
        {
            Id = string.Empty,
            Content = string.Empty
        };

        summary.Id.Should().Be(string.Empty);  
        summary.Content.Should().Be(string.Empty);
    }
}