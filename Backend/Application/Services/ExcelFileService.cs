using System.Data;
using System.Text;
using ExcelDataReader;
using Domain.Application;
using Domain.Persistence.DTOs;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace Application.Services;

public interface IExcelFileService
{
    Task<SummarizedExcelData> PrepareExcelFileForLLMAsync(IFormFile file, CancellationToken cancellationToken = default);
}

/// <summary>
/// ExcelFileService is responsible for preparing Excel files for the LLM
/// It reads the Excel file, extracts the data, and calculates summary statistics
/// The data and summary statistics are stored in the database for querying by the LLM model
/// The data is translated into vectors and stored in the database for querying by the LLM model
/// The vectors are compared against the vector that the LLM generates for the question to find the most relevant rows
/// It will use the most relevant rows to answer the question or provide the data
/// </summary>
public class ExcelFileService : IExcelFileService
{
    /// <summary>
    /// Prepare the given Excel file for LLM by reading the file and extracting the data and summary statistics
    /// This method reads the Excel file, extracts the data into a list of rows, and calculates summary statistics for the data
    /// This data will be translated into vectors and stored in the database for querying by the LLM model. The vectors will be
    /// compared against the vector that the LLM generates for the question to find the most relevant rows. It will use the most relevant
    /// rows to answer the question or provide the data whatever it may be.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public async Task<SummarizedExcelData> PrepareExcelFileForLLMAsync(IFormFile file, CancellationToken cancellationToken = default)
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
        var rows = new ConcurrentBag<Dictionary<string, object>>();
        var concurrentTable = new ConcurrentBag<DataRow>(table.AsEnumerable());
        var rowTask = Task.Run(async () =>
        {
            await Parallel.ForEachAsync(concurrentTable, parallelOptions, (row, token) =>
            {
                rows.Add(table.Columns.Cast<DataColumn>().ToDictionary(col => col.ColumnName, col => row[col] == DBNull.Value ? null! : row[col]));
                return ValueTask.CompletedTask;
            });
        }, parallelOptions.CancellationToken);
        var summaryTask = CalculateSummaryStatisticsAsync(table, parallelOptions);
        await Task.WhenAll(rowTask, summaryTask);
        return new SummarizedExcelData
        {
            RelevantRows = [.. rows],
            Summary = summaryTask.Result
        };
    }

    /// <summary>
    /// Calculate summary statistics for the given table
    /// This method calculates the sum, average, min, and max for each numeric column in the table.
    /// </summary>
    /// <param name="table"></param>
    /// <param name="parallelOptions"></param>
    /// <returns></returns>
    private static async Task<Dictionary<string, object>> CalculateSummaryStatisticsAsync(
        DataTable table, 
        ParallelOptions parallelOptions
    ) =>
        await Task.Run(() =>
        {
            var summary = new ExcelFileSummary
            {
                RowCount = table.Rows.Count,
                ColumnCount = table.Columns.Count,
                Columns = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList()
            };
            var stringColumns = GetStringColumns(table);
            var numericColumns = GetNumericColumns(table);
            Parallel.Invoke(parallelOptions,
                async () => await CalculateStringColumnHashes(table, stringColumns, summary, parallelOptions),
                async () => await CalculateNumericColumnStatistics(table, numericColumns, summary, parallelOptions)
            );
            return new Dictionary<string, object> { { "Summary", summary } };
        }, parallelOptions.CancellationToken);

    /// <summary>
    /// Get the numeric columns from the given table
    /// </summary>
    /// <param name="table"></param>
    /// <returns></returns>
    private static List<DataColumn> GetNumericColumns(DataTable table) => 
        table.Columns.Cast<DataColumn>()
            .Where(IsNumericColumn)
            .ToList();

    /// <summary>
    /// Check if the given column is of a numeric type
    /// </summary>
    /// <param name="column"></param>
    /// <returns></returns>
    private static bool IsNumericColumn(DataColumn column) =>
        column.DataType == typeof(int) || 
        column.DataType == typeof(double) || 
        column.DataType == typeof(float) || 
        column.DataType == typeof(decimal) || 
        column.DataType == typeof(long);

    /// <summary>
    /// Calculate the sum, average, min, and max for each numeric column in the given table
    /// Parallelizes the calculation of each statistic for each numeric column
    /// </summary>
    /// <param name="table"></param>
    /// <param name="numericColumns"></param>
    /// <param name="summary"></param>
    /// <param name="parallelOptions"></param>
    private static async Task CalculateNumericColumnStatistics(
        DataTable table, 
        List<DataColumn> numericColumns, 
        ExcelFileSummary summary, 
        ParallelOptions parallelOptions
    )
    {
        await Parallel.ForEachAsync(numericColumns, parallelOptions, async (column, token) =>
        {
            await Task.Run(() => {
                var columnName = column.ColumnName;
                var values = table.AsEnumerable()
                    .Where(row => row[column] != DBNull.Value)
                    .Select(row => Convert.ToDouble(row[column]))
                    .ToList();
                if (values.Count > 0)
                {
                    summary.Sums[columnName] = values.Sum();
                    summary.Mins[columnName] = values.Min();
                    summary.Maxs[columnName] = values.Max();
                    summary.Averages[columnName] = values.Average();
                }
            }, token);
        });
    }

    /// <summary>
    /// Get the string columns from the given table
    /// </summary>
    /// <param name="table"></param>
    /// <returns></returns>
    private static List<DataColumn> GetStringColumns(DataTable table) => 
        table.Columns.Cast<DataColumn>()
            .Where(IsStringColumn)
            .ToList();

    private static bool IsStringColumn(DataColumn column) => column.DataType == typeof(string);

    /// <summary>
    /// Calculate the SHA256 hash of each string value in the given columns
    /// Parallelizes the hashing of each string value in the given columns
    /// </summary>
    /// <param name="table"></param>
    /// <param name="stringColumns"></param>
    /// <param name="summary"></param>
    /// <param name="parallelOptions"></param>
    private static async Task CalculateStringColumnHashes(
        DataTable table, 
        List<DataColumn> stringColumns, 
        ExcelFileSummary summary, 
        ParallelOptions parallelOptions)
    {
        await Parallel.ForEachAsync(stringColumns, parallelOptions, async (column, token) =>
        {
            await Task.Run(() => {
                var hashedValues = new Dictionary<string, string>();
                table.AsEnumerable()
                    .Select(row => row[column]?.ToString())
                    .Where(value => !string.IsNullOrEmpty(value))
                    .ToList()
                    .ForEach(value => hashedValues[value!] = ComputeHash(value!));
                summary.HashedStrings[column.ColumnName] = hashedValues;
            }, token);
        });
    }

    /// <summary>
    /// Compute the SHA256 hash of the given input
    /// Should change it the same way every time
    /// </summary>
    /// <param name="input"></param>
    /// <returns>
    ///     The SHA256 hash of the input
    /// </returns>
    private static string ComputeHash(string input) => string.Concat(SHA256.HashData(Encoding.UTF8.GetBytes(input)).Select(b => b.ToString("x2")));
}