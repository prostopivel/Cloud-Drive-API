namespace FileMetadata.Core.Interfaces.Services
{
    public interface IFileMetadataService
    {
        Task<Entities.FileMetadata> CreateFileMetadataAsync(Guid fileId, string fileName,
            string originalName, long size, string contentType, Guid userId, string storagePath);
        Task<Entities.FileMetadata?> GetFileMetadataAsync(Guid fileId);
        Task<IEnumerable<Entities.FileMetadata>> GetUserFilesAsync(Guid userId);
        Task<bool> DeleteFileMetadataAsync(Guid fileId);
        Task<bool> BelongsToUserAsync(Guid fileId, Guid userId);
        Task UpdateLastAccessedAsync(Guid fileId);
    }
}
