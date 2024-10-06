namespace Application.Models;

public class ExcelFileSummary
{
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public required List<string> Columns { get; set; }
    public Dictionary<string, double> Sums { get; set; } = [];
    public Dictionary<string, double> Averages { get; set; } = [];
    public Dictionary<string, double> Mins { get; set; } = [];
    public Dictionary<string, double> Maxs { get; set; } = [];
    public Dictionary<string, Dictionary<string, string>> HashedStrings { get; set; } = [];
}