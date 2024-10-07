
namespace Domain.Persistence.DTOs;

public class SummarizedExcelData
{
    public required Dictionary<string, object> Summary { get; init; }
    public required List<Dictionary<string, object>> RelevantRows { get; init; }
}