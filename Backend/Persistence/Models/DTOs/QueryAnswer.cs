namespace Persistence.Models.DTOs;

public class QueryAnswer
{
    public required string Answer { get; set; }

    public string Question { get; set; } = "";

    public string DocumentId { get; set; } = "";

    public List<Dictionary<string, object>> RelevantRows { get; set; } = [];
}