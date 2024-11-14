namespace API.DTOs;

public class InitializeUploadRequest
{
    public string Filename { get; set; } = string.Empty;
}

public class InitializeUploadResponse : BaseResponse
{
}
