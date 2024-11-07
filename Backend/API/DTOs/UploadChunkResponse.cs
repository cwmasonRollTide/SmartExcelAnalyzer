namespace API.DTOs;

public class UploadResponse : BaseResponse
{
    public int ChunkSize { get; set; }
    public int ChunkCount { get; set; }
    public int ChunkIndex { get; set; }
    public int ChunkOffset { get; set; }
    public int ChunkLength { get; set; }
}
