using System.Diagnostics.CodeAnalysis;

namespace Domain.Persistence.Configuration;

[ExcludeFromCodeCoverage]
public class LLMServiceOptions
{
    public int COMPUTE_BATCH_SIZE { get; set; } = 100;
    public string LLM_SERVICE_URL { get; set; } = string.Empty;
}