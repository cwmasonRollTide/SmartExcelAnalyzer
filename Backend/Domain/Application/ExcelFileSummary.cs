using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Domain.Application;

[ExcludeFromCodeCoverage]
public class ExcelFileSummary
{
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public required List<string> Columns { get; set; }
    public ConcurrentDictionary<string, double> Sums { get; set; } = [];
    public ConcurrentDictionary<string, double> Mins { get; set; } = [];
    public ConcurrentDictionary<string, double> Maxs { get; set; } = [];
    public ConcurrentDictionary<string, double> Averages { get; set; } = [];
    public ConcurrentDictionary<string, ConcurrentDictionary<string, string>> HashedStrings { get; set; } = [];
}