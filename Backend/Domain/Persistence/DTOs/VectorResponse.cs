
using Domain.Application;

namespace Domain.Persistence.DTOs;

public class VectorQueryResponse
{
    public required Dictionary<string, object> Summary { get; init; }
    public required List<Dictionary<string, object>> RelevantRows { get; init; }
}