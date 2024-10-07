namespace Domain.Persistence.DTOs;

public class QueryAnswer
{

    public string Question { get; set; } = "";

    public string DocumentId { get; set; } = "";
    public required string Answer { get; set; }

    public List<Dictionary<string, object>> RelevantRows { get; set; } = [];
}