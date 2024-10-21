using Domain.Persistence.DTOs;
using System.Collections.Concurrent;

namespace SmartExcelAnalyzer.Tests.Domain.Persistence.DTOs;

public class SummarizedExcelDataTests
{
    [Fact]
    public void SummarizedExcelData_DefaultValues_ShouldBeNull()
    {
        var summarizedData = new SummarizedExcelData();

        Assert.Null(summarizedData.Summary);
        Assert.Null(summarizedData.Rows);
    }

    [Fact]
    public void SummarizedExcelData_CustomValues_ShouldSetCorrectly()
    {
        var summary = new ConcurrentDictionary<string, object>();
        summary["TotalRows"] = 100;
        summary["TotalColumns"] = 5;

        var rows = new ConcurrentBag<ConcurrentDictionary<string, object>>();
        var row1 = new ConcurrentDictionary<string, object>();
        row1["Column1"] = "Value1";
        row1["Column2"] = 42;
        rows.Add(row1);

        var summarizedData = new SummarizedExcelData
        {
            Summary = summary,
            Rows = rows
        };

        Assert.NotNull(summarizedData.Summary);
        Assert.Equal(100, summarizedData.Summary["TotalRows"]);
        Assert.Equal(5, summarizedData.Summary["TotalColumns"]);

        Assert.NotNull(summarizedData.Rows);
        Assert.Single(summarizedData.Rows);
        var firstRow = summarizedData.Rows.First();
        Assert.Equal("Value1", firstRow["Column1"]);
        Assert.Equal(42, firstRow["Column2"]);
    }

    [Fact]
    public void SummarizedExcelData_ConcurrentAccess_ShouldBeThreadSafe()
    {
        var summarizedData = new SummarizedExcelData
        {
            Summary = new ConcurrentDictionary<string, object>(),
            Rows = new ConcurrentBag<ConcurrentDictionary<string, object>>()
        };

        Parallel.For(0, 1000, i =>
        {
            summarizedData.Summary[$"Key{i}"] = $"Value{i}";
            var row = new ConcurrentDictionary<string, object>();
            row[$"Column{i}"] = i;
            summarizedData.Rows.Add(row);
        });

        Assert.Equal(1000, summarizedData.Summary.Count);
        Assert.Equal(1000, summarizedData.Rows.Count);

        for (int i = 0; i < 1000; i++)
        {
            Assert.True(summarizedData.Summary.ContainsKey($"Key{i}"));
            Assert.Equal($"Value{i}", summarizedData.Summary[$"Key{i}"]);
            Assert.Contains(summarizedData.Rows, row => row.ContainsKey($"Column{i}") && (int)row[$"Column{i}"] == i);
        }
    }
}