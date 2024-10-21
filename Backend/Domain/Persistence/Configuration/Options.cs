namespace Domain.Persistence.Configuration;

public class LLMServiceOptions
{
    public int COMPUTE_BATCH_SIZE { get; set; } = 100;
    public List<string> LLM_SERVICE_URLS { get; set; } = [];
    public string LLM_SERVICE_URL { get; set; } = string.Empty;
}

public class DatabaseOptions
{
    public int PORT { get; set; }
    public int SAVE_BATCH_SIZE { get; set; }
    public int MAX_RETRY_COUNT { get; set; }
    public bool USE_HTTPS { get; set; } = false;
    public int MAX_CONNECTION_COUNT { get; set; } = 10;
    public string HOST { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string QDRANT_API_KEY { get; set; } = string.Empty;
    public string CollectionName { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;    
    public string CollectionNameTwo { get; set; } = string.Empty;
}