using FluentAssertions;
using Domain.Application;
using System.Collections.Concurrent;

namespace SmartExcelAnalyzer.Tests.Domain.Application;

public class ExcelFileSummaryTests
{
    [Fact]
    public void ExcelFileSummary_Properties_ShouldSetAndGetCorrectly()
    {        
        var summary = new ExcelFileSummary
        {
            RowCount = 100,
            ColumnCount = 5,
            Columns = ["A", "B", "C", "D", "E"]
        };
        
        summary.Sums["A"] = 500.0;
        summary.Mins["B"] = 10.0;
        summary.Maxs["C"] = 1000.0;
        summary.Averages["D"] = 50.0;
        summary.HashedStrings["E"] = new ConcurrentDictionary<string, string>();
        summary.HashedStrings["E"]["hash1"] = "value1";

        summary.Columns.Should().BeEquivalentTo(["A", "B", "C", "D", "E" ]);
        summary.RowCount.Should().Be(100);
        summary.Columns.Should().HaveCount(5);
        summary.Sums.Should().ContainKey("A").WhoseValue.Should().Be(500.0);
        summary.Mins.Should().ContainKey("B").WhoseValue.Should().Be(10.0);
        summary.Maxs.Should().ContainKey("C").WhoseValue.Should().Be(1000.0);
        summary.Averages.Should().ContainKey("D").WhoseValue.Should().Be(50.0);
        summary.HashedStrings.Should().ContainKey("E").WhoseValue.Should().ContainKey("hash1").WhoseValue.Should().Be("value1");
    }

    [Fact]
    public void ExcelFileSummary_Dictionaries_ShouldBeInitialized()
    {
        var summary = new ExcelFileSummary
        {
            Columns = []
        };
        summary.Sums.Should().NotBeNull();
        summary.Mins.Should().NotBeNull();
        summary.Maxs.Should().NotBeNull();
        summary.Averages.Should().NotBeNull();
        summary.HashedStrings.Should().NotBeNull();
    }
}