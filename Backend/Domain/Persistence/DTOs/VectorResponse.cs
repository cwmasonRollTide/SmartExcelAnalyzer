using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Domain.Persistence.DTOs;

[ExcludeFromCodeCoverage]
public class SummarizedExcelData
{
    public ConcurrentDictionary<string, object>? Summary { get; init; }
    public ConcurrentBag<ConcurrentDictionary<string, object>>? Rows { get; init; }
}