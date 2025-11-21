using FileMetadata.Core.Interfaces.Repositories;
using FileMetadata.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FileMetadata.Infrastructure.Repositories
{
    public class FileMetadataRepository : IFileMetadataRepository
    {
        private readonly FileMetadataDbContext _context;

        public FileMetadataRepository(FileMetadataDbContext context)
        {
            _context = context;
        }

        public async Task<Core.Entities.FileMetadata?> GetByIdAsync(Guid fileId)
        {
            return await _context.FileMetadata
                .SingleOrDefaultAsync(f => f.Id == fileId);
        }

        public async Task<IEnumerable<Core.Entities.FileMetadata>> GetByUserIdAsync(Guid userId)
        {
            return await _context.FileMetadata
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();
        }

        public async Task<Core.Entities.FileMetadata> AddAsync(Core.Entities.FileMetadata fileMetadata)
        {
            _context.FileMetadata.Add(fileMetadata);
            await _context.SaveChangesAsync();
            return fileMetadata;
        }

        public async Task UpdateAsync(Core.Entities.FileMetadata fileMetadata)
        {
            _context.FileMetadata.Update(fileMetadata);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(Guid fileId)
        {
            var fileMetadata = await _context.FileMetadata
                .SingleOrDefaultAsync(f => f.Id == fileId);

            if (fileMetadata == null)
            {
                return false;
            }

            fileMetadata.MarkAsDeleted();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid fileId)
        {
            return await _context.FileMetadata
                .AnyAsync(f => f.Id == fileId);
        }

        public async Task<bool> BelongsToUserAsync(Guid fileId,
            Guid userId)
        {
            return await _context.FileMetadata
                .AnyAsync(f => f.Id == fileId && f.UserId == userId);
        }

        public async Task UpdateLastAccessedAsync(Guid fileId)
        {
            var fileMetadata = await _context.FileMetadata
                .SingleOrDefaultAsync(f => f.Id == fileId);

            if (fileMetadata != null)
            {
                fileMetadata.MarkAsAccessed();
                await _context.SaveChangesAsync();
            }
        }
    }
}
