namespace API.DTOs;

public class FinalizeUploadResponse
{
    public string DocumentId { get; set; } = string.Empty;
    public string? Filename { get; set; }
}