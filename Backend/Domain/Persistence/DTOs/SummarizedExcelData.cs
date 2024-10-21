using System.Collections.Concurrent;

namespace Domain.Persistence.DTOs;

public class SummarizedExcelData
{
    public ConcurrentDictionary<string, object>? Summary { get; init; }
    public ConcurrentBag<ConcurrentDictionary<string, object>>? Rows { get; init; }
}