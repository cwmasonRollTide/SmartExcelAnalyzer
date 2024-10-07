namespace Domain.Persistence.DTOs;

public class VectorResponse
{
    public required Dictionary<string, object> Summary { get; init; }
    public required List<Dictionary<string, object>> RelevantRows { get; init; }
}