using Domain.Persistence;

namespace SmartExcelAnalyzer.Tests.Domain.Persistence;

public class SummaryTests
{
    [Fact]
    public void Summary_Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var summary = new Summary
        {
            Id = "sum123",
            Content = "This is a test summary"
        };

        // Act & Assert
        Assert.Equal("sum123", summary.Id);
        Assert.Equal("This is a test summary", summary.Content);
    }

    [Fact]
    public void Summary_Properties_ShouldAllowEmptyStrings()
    {
        // Arrange
        var summary = new Summary
        {
            Id = string.Empty,
            Content = string.Empty
        };

        // Act & Assert
        Assert.Equal(string.Empty, summary.Id);
        Assert.Equal(string.Empty, summary.Content);
    }
}