namespace API.DTOs;

public class BaseResponse
{
    public string DocumentId { get; set; } = string.Empty;
    public string? Filename { get; set; }
}