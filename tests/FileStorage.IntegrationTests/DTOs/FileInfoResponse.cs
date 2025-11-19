namespace FileStorage.IntegrationTests.DTOs
{
    public record FileInfoResponse
    {
        public Guid FileId { get; init; }
        public string FileName { get; init; } = string.Empty;
        public long Size { get; init; }
        public string ContentType { get; init; } = string.Empty;
        public DateTime UploadedAt { get; init; }
    }
}
