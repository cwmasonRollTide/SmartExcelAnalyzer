using System.Data;
using System.Text;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace Application.Services;

public interface IExcelFileService
{
    Task<(List<Dictionary<string, object>> Rows, Dictionary<string, object> Summary)>  PrepareExcelFileForLLMAsync(IFormFile file);
}

public class ExcelFileService : IExcelFileService
{
    public ExcelFileService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<(List<Dictionary<string, object>> Rows, Dictionary<string, object> Summary)> PrepareExcelFileForLLMAsync(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var result = reader.AsDataSet(new ExcelDataSetConfiguration()
        {
            ConfigureDataTable = _ => new()
            {
                UseHeaderRow = true
            }
        });
        var table = result.Tables[0];
        var rows = new ConcurrentBag<Dictionary<string, object>>();//Thread safe collectionsssss
        var rowTask = Task.Run(() =>
        {
            Parallel.ForEach(table.AsEnumerable(), row =>
            {
                rows.Add(table.Columns.Cast<DataColumn>().ToDictionary(col => col.ColumnName, col => row[col] == DBNull.Value ? null! : row[col]));
            });
        });
        var summaryTask = CalculateSummaryStatisticsAsync(table);
        await Task.WhenAll(rowTask, summaryTask);
        return (Rows: [.. rows], Summary: summaryTask.Result);//When am I gonna get used to spread operators in C#? sometimes I just do as resharper tells me
    }

    private static async Task<Dictionary<string, object>> CalculateSummaryStatisticsAsync(DataTable table)
    {
        return await Task.Run(() =>
        {
            var summary = new Dictionary<string, object>
            {
                ["RowCount"] = table.Rows.Count,
                ["ColumnCount"] = table.Columns.Count,
                ["Columns"] = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList()
            };
            var numericColumns = table.Columns.Cast<DataColumn>()
                .Where(c => c.DataType == typeof(int) || c.DataType == typeof(double) || c.DataType == typeof(float))
                .ToList();
            foreach (var column in numericColumns)
            {
                var columnName = column.ColumnName;
                var values = table.AsEnumerable().Select(row => Convert.ToDouble(row[column])).ToList();
                summary[$"{columnName}_Sum"] = values.Sum();
                summary[$"{columnName}_Average"] = values.Average();
                summary[$"{columnName}_Min"] = values.Min();
                summary[$"{columnName}_Max"] = values.Max();
            }
            return summary;
        });
    }
}