namespace FileStorage.IntegrationTests.DTOs
{
    public record FileUploadResponse
    {
        public Guid FileId { get; init; }
        public string FileName { get; init; } = string.Empty;
        public long Size { get; init; }
        public DateTime UploadedAt { get; init; }
    }
}
