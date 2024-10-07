using System.Diagnostics.CodeAnalysis;

namespace Domain.Persistence.Configuration;

[ExcludeFromCodeCoverage]
public class LLMServiceOptions
{
    public string LLM_SERVICE_URL { get; set; } = string.Empty;
}