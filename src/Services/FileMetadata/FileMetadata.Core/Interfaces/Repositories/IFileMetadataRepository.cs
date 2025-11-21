namespace FileMetadata.Core.Interfaces.Repositories
{
    public interface IFileMetadataRepository
    {
        Task<Entities.FileMetadata?> GetByIdAsync(Guid fileId);
        Task<IEnumerable<Entities.FileMetadata>> GetByUserIdAsync(Guid userId);
        Task<Entities.FileMetadata> AddAsync(Entities.FileMetadata fileMetadata);
        Task UpdateAsync(Entities.FileMetadata fileMetadata);
        Task<bool> DeleteAsync(Guid fileId);
        Task<bool> ExistsAsync(Guid fileId);
        Task<bool> BelongsToUserAsync(Guid fileId, Guid userId);
        Task UpdateLastAccessedAsync(Guid fileId);
    }
}
