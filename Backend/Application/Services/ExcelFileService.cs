using System.Data;
using System.Text;
using ExcelDataReader;
using Application.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace Application.Services;

public interface IExcelFileService
{
    Task<(List<Dictionary<string, object>> Rows, ExcelFileSummary Summary)> PrepareExcelFileForLLMAsync(IFormFile file, CancellationToken cancellationToken = default);
}

public class ExcelFileService : IExcelFileService
{
    public ExcelFileService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Prepare the given Excel file for LLM by reading the file and extracting the data and summary statistics
    /// This method reads the Excel file, extracts the data into a list of rows, and calculates summary statistics for the data
    /// This data will be translated into vectors and stored in the database for querying by the LLM model. The vectors will be
    /// compared against the vector that the LLM generates for the question to find the most relevant rows. It will use the most relevant
    /// rows to answer the question or provide the data whatever it may be.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public async Task<(List<Dictionary<string, object>> Rows, ExcelFileSummary Summary)> PrepareExcelFileForLLMAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var parallelOptions = new ParallelOptions { CancellationToken = cancellationToken };
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
            Parallel.ForEach(table.AsEnumerable(), parallelOptions, row =>
            {
                rows.Add(table.Columns.Cast<DataColumn>().ToDictionary(col => col.ColumnName, col => row[col] == DBNull.Value ? null! : row[col]));
            });
        }, parallelOptions.CancellationToken);
        var summaryTask = CalculateSummaryStatisticsAsync(table, parallelOptions);
        await Task.WhenAll(rowTask, summaryTask);
        return (Rows: [.. rows], Summary: summaryTask.Result);//When am I gonna get used to spread operators in C#? sometimes I just do as resharper tells me
    }

    /// <summary>
    /// Calculate summary statistics for the given table
    /// This method calculates the sum, average, min, and max for each numeric column in the table.
    /// </summary>
    /// <param name="table"></param>
    /// <returns></returns>
    private static async Task<ExcelFileSummary> CalculateSummaryStatisticsAsync(DataTable table, ParallelOptions parallelOptions) =>
        await Task.Run(() =>
        {
            var summary = new ExcelFileSummary
            {
                RowCount = table.Rows.Count,
                ColumnCount = table.Columns.Count,
                Columns = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList()
            };

            var numericColumns = GetNumericColumns(table);
            var stringColumns = GetStringColumns(table);

            Parallel.Invoke(parallelOptions,
                () => CalculateNumericColumnStatistics(table, numericColumns, summary),
                () => CalculateStringColumnHashes(table, stringColumns, summary)
            );

            return summary;
        }, parallelOptions.CancellationToken);

    private static List<DataColumn> GetNumericColumns(DataTable table) => table.Columns.Cast<DataColumn>()
            .Where(c => c.DataType == typeof(int) || c.DataType == typeof(double) || c.DataType == typeof(float))
            .ToList();

    /// <summary>
    /// Calculate the sum, average, min, and max for each numeric column in the given table
    /// Parallelizes the calculation of each statistic for each numeric column
    /// </summary>
    /// <param name="table"></param>
    /// <param name="numericColumns"></param>
    /// <param name="summary"></param>
    private static void CalculateNumericColumnStatistics(DataTable table, List<DataColumn> numericColumns, ExcelFileSummary summary)
    {
        Parallel.ForEach(numericColumns, column =>
        {
            var columnName = column.ColumnName;
            var values = table.AsEnumerable()
                .Where(row => row[column] != DBNull.Value)
                .Select(row => Convert.ToDouble(row[column]))
                .ToList();

            if (values.Count > 0)
            {
                summary.Sums[columnName] = values.Sum();
                summary.Averages[columnName] = values.Average();
                summary.Mins[columnName] = values.Min();
                summary.Maxs[columnName] = values.Max();
            }
        });
    }

    private static List<DataColumn> GetStringColumns(DataTable table) => table.Columns.Cast<DataColumn>()
            .Where(c => c.DataType == typeof(string))
            .ToList();

    /// <summary>
    /// Calculate the SHA256 hash of each string value in the given columns
    /// Parallelizes the hashing of each string value in the given columns
    /// </summary>
    /// <param name="table"></param>
    /// <param name="stringColumns"></param>
    /// <param name="summary"></param>
    private static void CalculateStringColumnHashes(DataTable table, List<DataColumn> stringColumns, ExcelFileSummary summary)
    {
        Parallel.ForEach(stringColumns, column =>
        {
            var columnName = column.ColumnName;
            var hashedValues = new Dictionary<string, string>();

            foreach (var row in table.AsEnumerable())
            {
                var value = row[column]?.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    var hashedValue = ComputeHash(value);
                    hashedValues[value] = hashedValue;
                }
            }

            summary.HashedStrings[columnName] = hashedValues;
        });
    }

    /// <summary>
    /// Compute the SHA256 hash of the given input
    /// Should change it the same way every time
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
}