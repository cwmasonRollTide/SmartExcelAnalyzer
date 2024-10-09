using System.Diagnostics.CodeAnalysis;

namespace Domain.Persistence.Configuration;

[ExcludeFromCodeCoverage]
public class LLMServiceOptions
{
    public int COMPUTE_BATCH_SIZE { get; set; } = 100;
    public string LLM_SERVICE_URL { get; set; } = string.Empty;
}

[ExcludeFromCodeCoverage]
public class DatabaseOptions
{
    public int SAVE_BATCH_SIZE { get; set; } = 100;
    public int MAX_CONNECTION_COUNT { get; set; } = 400;
    public int MAX_RETRY_COUNT { get; set; } = 3;
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}