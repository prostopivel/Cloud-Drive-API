namespace FileMetadata.Core.Interfaces.Repositories
{
    public interface IFileMetadataRepository
    {
        Task<Entities.FileMetadata> GetByIdAsync(Guid fileId, CancellationToken token = default);
        Task<IEnumerable<Entities.FileMetadata>> GetByUserIdAsync(Guid userId, CancellationToken token = default);
        Task<Entities.FileMetadata> AddAsync(Entities.FileMetadata fileMetadata, CancellationToken token = default);
        Task UpdateAsync(Entities.FileMetadata fileMetadata, CancellationToken token = default);
        Task DeleteAsync(Guid fileId, CancellationToken token = default);
        Task<bool> ExistsAsync(Guid fileId, CancellationToken token = default);
        Task<bool> BelongsToUserAsync(Guid fileId, Guid userId, CancellationToken token = default);
        Task UpdateLastAccessedAsync(Guid fileId, CancellationToken token = default);
    }
}
