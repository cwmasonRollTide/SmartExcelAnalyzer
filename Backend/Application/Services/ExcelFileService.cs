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
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };
        var excelData = LoadExcelData(file);
        var table = excelData.Tables[0]; // Assuming we're only processing the first sheet
        var rows = new ConcurrentBag<ConcurrentDictionary<string, object>>();
        var addingRowsTask = Parallel.ForEachAsync(table.AsEnumerable(), parallelOptions, (row, token) =>
        {
            rows.Add(new ConcurrentDictionary<string, object>(table.Columns.Cast<DataColumn>()
                .ToDictionary(
                    col => col.ColumnName,
                    col => row[col] == DBNull.Value ? null! : row[col]
                )
            ));
            return ValueTask.CompletedTask;
        });
        var summaryTask = CalculateSummaryStatisticsAsync(table, parallelOptions);
        await Task.WhenAll(addingRowsTask, summaryTask);
        return new SummarizedExcelData
        {
            Rows = [.. rows],
            Summary = summaryTask.Result
        };
    }

    /// <summary>
    /// Load the data from the given Excel file
    /// This method reads the Excel file and returns the data as a DataSet
    /// Utilizes the ExcelDataReader library known for its speed and efficiency
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    private static DataSet LoadExcelData(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var reader = ExcelReaderFactory.CreateReader(stream);
        return reader.AsDataSet(new ExcelDataSetConfiguration()
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration()
            {
                UseHeaderRow = true,
                ReadHeaderRow = rowReader =>
                {
                    var columnNames = new List<string>();
                    for (int i = 0; i < rowReader.FieldCount; i++)
                    {
                        columnNames.Add(rowReader.GetValue(i)?.ToString() ?? $"Column{i + 1}"); // If the column name is null, use a default name of Column#
                    }
                    columnNames.ToArray();
                }
            }
        });
    }

    /// <summary>
    /// Calculate summary statistics for the given table
    /// This method calculates the sum, average, min, and max for each numeric column in the table.
    /// </summary>
    /// <param name="table"></param>
    /// <param name="parallelOptions"></param>
    /// <returns></returns>
    private static async Task<ConcurrentDictionary<string, object>> CalculateSummaryStatisticsAsync(DataTable table, ParallelOptions parallelOptions)
    {
        var summary = new ExcelFileSummary
        {
            RowCount = table.Rows.Count,
            ColumnCount = table.Columns.Count,
            Columns = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList()
        };
        var calculatinStringColumnHashes = CalculateStringColumnHashes(table, GetStringColumns(table), summary, parallelOptions);
        var calculatingNumericColumnStats = CalculateNumericColumnStatistics(table, GetNumericColumns(table), summary, parallelOptions);
        await Task.WhenAll(calculatinStringColumnHashes, calculatingNumericColumnStats);
        var result = new ConcurrentDictionary<string, object>();
        result["Summary"] = summary;
        return result;
    }

    /// <summary>
    /// Get the numeric columns from the given table
    /// </summary>
    /// <param name="table"></param>
    /// <returns></returns>
    private static ConcurrentBag<DataColumn> GetNumericColumns(DataTable table) => new(table.Columns.Cast<DataColumn>().Where(c => IsNumericColumn(c, table)));

    /// <summary>
    /// Check if the given column is of a numeric type
    /// </summary>
    /// <param name="column"></param>
    /// <returns></returns>
    private static bool IsNumericColumn(DataColumn column, DataTable table)
    {
        if (column.DataType == typeof(int) || column.DataType == typeof(double) ||
            column.DataType == typeof(float) || column.DataType == typeof(decimal) ||
            column.DataType == typeof(long))
        {
            return true;
        }
        return table.AsEnumerable()
            .Where(row => row[column] != DBNull.Value)
            .All(row => double.TryParse(row[column].ToString(), out _));
    }

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
        ConcurrentBag<DataColumn> numericColumns,
        ExcelFileSummary summary,
        ParallelOptions parallelOptions
    ) =>
        await Parallel.ForEachAsync(numericColumns, parallelOptions, (column, cancellationToken) =>
        {
            var columnName = column.ColumnName;
            var values = table.AsEnumerable()
                .Where(row => row[column] != DBNull.Value)
                .Select(row => Convert.ToDouble(row[column]));
            if (values.Any())
            {
                summary.Sums[columnName] = values.Sum();
                summary.Mins[columnName] = values.Min();
                summary.Maxs[columnName] = values.Max();
                summary.Averages[columnName] = values.Average();
            }
            return ValueTask.CompletedTask;
        });

    /// <summary>
    /// Get the string columns from the given table
    /// </summary>
    /// <param name="table"></param>
    /// <returns></returns>
    private static ConcurrentBag<DataColumn> GetStringColumns(DataTable table) => new(table.Columns.Cast<DataColumn>().Where(IsStringColumn));
    private static bool IsStringColumn(DataColumn column) => column.DataType == typeof(string);

    /// <summary>
    /// Calculate the hash values of each string column's values
    /// So for each column that is considered a string column, 
    /// we are taking the whole column and calculating the hash of each value
    /// </summary>
    /// <param name="table"></param>
    /// <param name="stringColumns"></param>
    /// <param name="summary"></param>
    /// <param name="parallelOptions"></param>
    private static async Task CalculateStringColumnHashes(
        DataTable table,
        ConcurrentBag<DataColumn> stringColumns,
        ExcelFileSummary summary,
        ParallelOptions parallelOptions
    ) =>
        await Parallel.ForEachAsync(stringColumns, parallelOptions, (column, cancellationToken) =>
        {
            var hashedValues = table.AsEnumerable()
                .Select(row => row[column]?.ToString())
                .Where(value => !string.IsNullOrEmpty(value))
                .GroupBy(value => value)
                .ToDictionary(
                    g => g.Key!,
                    g => ComputeHash(g.Key!),
                    StringComparer.OrdinalIgnoreCase
                );
            summary.HashedStrings[column.ColumnName] = new ConcurrentDictionary<string, string>(hashedValues);
            return ValueTask.CompletedTask;
        });

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