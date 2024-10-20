using System.Diagnostics.CodeAnalysis;

namespace Domain.Persistence.Configuration;

[ExcludeFromCodeCoverage]
public class LLMServiceOptions
{
    public int COMPUTE_BATCH_SIZE { get; set; } = 100;
    public string LLM_SERVICE_URL { get; set; } = string.Empty;
    public List<string> LLM_SERVICE_URLS { get; set; } = [];
}

[ExcludeFromCodeCoverage]
public class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string HOST { get; set; } = string.Empty;
    public int PORT { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public bool UseHttps { get; set; } = false;
    public string CollectionName { get; set; } = string.Empty;
    public string CollectionNameTwo { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public int MAX_CONNECTION_COUNT { get; set; }
    public int SAVE_BATCH_SIZE { get; set; }
    public int MAX_RETRY_COUNT { get; set; }
}