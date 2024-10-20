using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using Microsoft.Extensions.Primitives;

namespace SmartExcelAnalyzer.Tests.TestUtilities;

public static class TestDataGenerator
{
    public static IFormFile GenerateExcelFile(int rowCount, List<string> headers, string fileName = "test.xlsx")
    {
        var allHeaders = new List<string>(headers);
        if (!allHeaders.Contains("embedding"))
        {
            allHeaders.Add("embedding");
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");

        // Add headers
        for (int i = 0; i < allHeaders.Count; i++)
        {
            worksheet.Cell(1, i + 1).Value = allHeaders[i];
        }

        // Add data
        var data = GenerateLargeDataSet(rowCount, allHeaders).ToList();
        for (int row = 0; row < data.Count; row++)
        {
            for (int col = 0; col < allHeaders.Count; col++)
            {
                var value = data[row][allHeaders[col]];
                if (value is float[] floatArray)
                {
                    worksheet.Cell(row + 2, col + 1).Value = string.Join(",", floatArray);
                }
                else
                {
                    worksheet.Cell(row + 2, col + 1).Value = value?.ToString() ?? string.Empty;
                }
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();
        var file = new FormFile(new MemoryStream(content), 0, content.Length, "data", fileName)
        {
            Headers = new HeaderDictionary(headers.ToDictionary(h => h, h => new StringValues(h))),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
        return file;
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
                    "document_id" => $"document_id_{i + 1}",
                    "content" => $"content_{i + 1}",
                    "embedding" => Enumerable.Range(0, 10).Select(_ => (float)Random.Shared.NextDouble()).ToArray(),
                    _ => $"{header}_{i + 1}",
                };
            }
            yield return row;
        }
    }
}