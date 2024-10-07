using System.Diagnostics.CodeAnalysis;

namespace Domain.Persistence;

[ExcludeFromCodeCoverage]
public class Document
{
    public required string Id { get; set; }
    public required string Content { get; set; }
    public required float[] Embedding { get; set; }
}