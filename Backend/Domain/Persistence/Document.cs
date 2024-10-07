namespace Domain.Persistence;

public class Document
{
    public required string Id { get; set; }
    public required string Content { get; set; }
    public required float[] Embedding { get; set; }
}