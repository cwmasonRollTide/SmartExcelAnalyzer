using System.Collections.Concurrent;

namespace Domain.Persistence.DTOs;

public class SummarizedExcelData
{
    public string FileName { get; set; } = string.Empty;
    public ConcurrentDictionary<string, object>? Summary { get; init; }
    public ConcurrentBag<ConcurrentDictionary<string, object>>? Rows { get; init; }
}