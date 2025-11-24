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

        public async Task<Core.Entities.FileMetadata> GetByIdAsync(Guid fileId,
            CancellationToken token = default)
        {
            return await _context.FileMetadata
                .SingleAsync(f => f.Id == fileId, cancellationToken: token);
        }

        public async Task<IEnumerable<Core.Entities.FileMetadata>> GetByUserIdAsync(Guid userId,
            CancellationToken token = default)
        {
            return await _context.FileMetadata
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync(cancellationToken: token);
        }

        public async Task<Core.Entities.FileMetadata> AddAsync(Core.Entities.FileMetadata fileMetadata,
            CancellationToken token = default)
        {
            _context.FileMetadata.Add(fileMetadata);
            await _context.SaveChangesAsync(cancellationToken: token);
            return fileMetadata;
        }

        public async Task UpdateAsync(Core.Entities.FileMetadata fileMetadata,
            CancellationToken token = default)
        {
            _context.FileMetadata.Update(fileMetadata);
            await _context.SaveChangesAsync(cancellationToken: token);
        }

        public async Task DeleteAsync(Guid fileId,
            CancellationToken token = default)
        {
            var fileMetadata = await _context.FileMetadata
                .SingleAsync(f => f.Id == fileId, cancellationToken: token);

            fileMetadata.MarkAsDeleted();
            await _context.SaveChangesAsync(cancellationToken: token);
        }

        public async Task<bool> ExistsAsync(Guid fileId,
            CancellationToken token = default)
        {
            return await _context.FileMetadata
                .AnyAsync(f => f.Id == fileId, cancellationToken: token);
        }

        public async Task<bool> BelongsToUserAsync(Guid fileId,
            Guid userId,
            CancellationToken token = default)
        {
            return await _context.FileMetadata
                .AnyAsync(f => f.Id == fileId && f.UserId == userId, cancellationToken: token);
        }

        public async Task UpdateLastAccessedAsync(Guid fileId,
            CancellationToken token = default)
        {
            var fileMetadata = await _context.FileMetadata
                .SingleOrDefaultAsync(f => f.Id == fileId, cancellationToken: token);

            if (fileMetadata != null)
            {
                fileMetadata.MarkAsAccessed();
                await _context.SaveChangesAsync(cancellationToken: token);
            }
        }
    }
}
