namespace Shared.Common.Models
{
    public class FileStorageSettings
    {
        public string StoragePath { get; set; } = "storage";
        public long MaxFileSize { get; set; } = 100 * 1024 * 1024; // 100MB
        public string[] AllowedExtensions { get; set; } = { ".pdf", ".jpg", ".jpeg", ".png", ".txt", ".zip" };
    }
}
