using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Domain.Persistence.DTOs;

[ExcludeFromCodeCoverage]
public class QueryAnswer
{
    public string Question { get; set; } = "";
    public string DocumentId { get; set; } = "";
    public required string Answer { get; set; }
    public ConcurrentBag<ConcurrentDictionary<string, object>> RelevantRows { get; set; } = [];
}