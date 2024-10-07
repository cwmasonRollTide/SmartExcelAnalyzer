using System.Diagnostics.CodeAnalysis;

namespace Domain.Persistence.DTOs;

[ExcludeFromCodeCoverage]
public class SummarizedExcelData
{
    public required Dictionary<string, object> Summary { get; init; }
    public required List<Dictionary<string, object>> RelevantRows { get; init; }
}