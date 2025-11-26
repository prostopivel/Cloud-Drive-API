using FileStorage.Core.Entities;

namespace FileStorage.Core.Interfaces.Repositories
{
    public interface IFileStorageRepository
    {
        Task<StoredFile> SaveFileAsync(Stream fileStream, string fileName,
            string contentType, long fileSize, Guid userId, CancellationToken token = default);
        Task<StoredFile?> GetFileAsync(Guid fileId, CancellationToken token = default);
        Task<bool> DeleteFileAsync(Guid fileId, CancellationToken token = default);
        Task<Stream> GetFileStreamAsync(Guid fileId, CancellationToken token = default);
    }
}
