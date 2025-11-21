namespace FileMetadata.Core.Entities
{
    public class FileMetadata
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalName { get; set; } = string.Empty;
        public long Size { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string StoragePath { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public FileMetadata() { }

        public FileMetadata(Guid id,
            string fileName,
            string originalName,
            long size,
            string contentType,
            Guid userId,
            string storagePath,
            DateTime uploadedAt)
        {
            Id = id;
            FileName = fileName;
            OriginalName = originalName;
            Size = size;
            ContentType = contentType;
            UserId = userId;
            StoragePath = storagePath;
            UploadedAt = uploadedAt;
            LastAccessedAt = uploadedAt;
        }

        public void MarkAsAccessed()
        {
            LastAccessedAt = DateTime.UtcNow;
        }

        public void MarkAsDeleted()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }
    }
}
