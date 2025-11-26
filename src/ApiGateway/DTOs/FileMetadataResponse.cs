namespace ApiGateway.DTOs
{
    public record FileMetadataResponse
    {
        public Guid FileId { get; init; }
        public string FileName { get; init; } = string.Empty;
        public long Size { get; init; }
        public string ContentType { get; init; } = string.Empty;
        public DateTime UploadedAt { get; init; }
        public DateTime? LastAccessedAt { get; init; }
    }
}
