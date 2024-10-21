using Domain.Application;
using System.Collections.Concurrent;

namespace SmartExcelAnalyzer.Tests.Domain.Application;

public class ExcelFileSummaryTests
{
    [Fact]
    public void ExcelFileSummary_Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var summary = new ExcelFileSummary
        {
            RowCount = 100,
            ColumnCount = 5,
            Columns = new List<string> { "A", "B", "C", "D", "E" }
        };

        // Act
        summary.Sums["A"] = 500.0;
        summary.Mins["B"] = 10.0;
        summary.Maxs["C"] = 1000.0;
        summary.Averages["D"] = 50.0;
        summary.HashedStrings["E"] = new ConcurrentDictionary<string, string>();
        summary.HashedStrings["E"]["hash1"] = "value1";

        // Assert
        Assert.Equal(100, summary.RowCount);
        Assert.Equal(5, summary.ColumnCount);
        Assert.Equal(new List<string> { "A", "B", "C", "D", "E" }, summary.Columns);
        Assert.Equal(500.0, summary.Sums["A"]);
        Assert.Equal(10.0, summary.Mins["B"]);
        Assert.Equal(1000.0, summary.Maxs["C"]);
        Assert.Equal(50.0, summary.Averages["D"]);
        Assert.Equal("value1", summary.HashedStrings["E"]["hash1"]);
    }

    [Fact]
    public void ExcelFileSummary_Dictionaries_ShouldBeInitialized()
    {
        // Arrange & Act
        var summary = new ExcelFileSummary
        {
            Columns = new List<string>()
        };

        // Assert
        Assert.NotNull(summary.Sums);
        Assert.NotNull(summary.Mins);
        Assert.NotNull(summary.Maxs);
        Assert.NotNull(summary.Averages);
        Assert.NotNull(summary.HashedStrings);
    }
}