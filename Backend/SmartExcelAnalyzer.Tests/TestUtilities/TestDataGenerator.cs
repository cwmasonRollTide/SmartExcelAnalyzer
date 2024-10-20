using Domain.Persistence.DTOs;
using System.Collections.Concurrent;

namespace SmartExcelAnalyzer.Tests.TestUtilities;

public static class TestDataGenerator
{
    public static SummarizedExcelData GenerateSummarizedExcelData(int rowCount, List<string> headers)
    {
        var summarizedData = new SummarizedExcelData
        {
            Summary = new ConcurrentDictionary<string, object>
            {
                ["RowCount"] = rowCount,
                ["ColumnCount"] = headers.Count,
                ["Columns"] = headers
            },
            Rows = []
        };

        var data = GenerateLargeDataSet(rowCount, headers).ToList();
        foreach (var row in data)
        {
            summarizedData.Rows.Add(new ConcurrentDictionary<string, object>(row));
        }

        summarizedData.Summary["Min"] = data!.Min(row => row["id"])!;
        summarizedData.Summary["Max"] = data!.Max(row => row["id"])!;
        summarizedData.Summary["Average"] = data.Average(row => 
        {
            var idString = row["id"]?.ToString()!.Split("_")[1];
            return int.TryParse(idString, out var id) ? id : 0;
        });
        summarizedData.Summary["Sum"] = data.Sum(row => 
        {
            var idString = row["id"]?.ToString()!.Split("_")[1];
            return int.TryParse(idString, out var id) ? id : 0;
        });

        return summarizedData;
    }

    public static IEnumerable<ConcurrentDictionary<string, object>> GenerateLargeDataSet(int count, List<string> headers)
    {
        for (int i = 0; i < count; i++)
        {
            var row = new ConcurrentDictionary<string, object>();
            foreach (var header in headers)
            {
                row[header] = header.ToLower() switch
                {
                    "id" => $"id_{i + 1}",
                    "data" => $"data_{i + 1}",
                    "embedding" => Enumerable.Range(0, 10).Select(_ => (float)Random.Shared.NextDouble()).ToArray(),
                    _ => $"{header}_{i + 1}",
                };
            }
            yield return row;
        }
    }
}