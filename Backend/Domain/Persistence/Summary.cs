using System.Diagnostics.CodeAnalysis;

namespace Domain.Persistence;

[ExcludeFromCodeCoverage]
public class Summary
{
    public required string Id { get; set; }
    public required string Content { get; set; }
}
