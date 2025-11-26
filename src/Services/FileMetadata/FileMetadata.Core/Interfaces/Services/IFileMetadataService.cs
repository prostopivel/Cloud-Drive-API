namespace FileMetadata.Core.Interfaces.Services
{
    public interface IFileMetadataService
    {
        Task<Entities.FileMetadata> CreateFileMetadataAsync(Guid fileId, string fileName,
            string originalName, long size, string contentType, Guid userId,
            string storagePath, CancellationToken token = default);
        Task<Entities.FileMetadata> GetFileMetadataAsync(Guid fileId, Guid userId, CancellationToken token = default);
        Task<IEnumerable<Entities.FileMetadata>> GetUserFilesAsync(Guid userId, CancellationToken token = default);
        Task DeleteFileMetadataAsync(Guid fileId, Guid userId, CancellationToken token = default);
        Task BelongsToUserAsync(Guid fileId, Guid userId, CancellationToken token = default);
    }
}
