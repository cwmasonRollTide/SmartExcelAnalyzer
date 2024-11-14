namespace API.DTOs;

public class FinalizeUploadRequest
{
    public string UploadId { get; set; } = string.Empty;
}

public class FinalizeUploadResponse : BaseResponse
{
}
