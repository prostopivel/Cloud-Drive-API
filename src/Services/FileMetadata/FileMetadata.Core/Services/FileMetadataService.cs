using FileMetadata.Core.Interfaces.Repositories;
using FileMetadata.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace FileMetadata.Core.Services
{
    public class FileMetadataService : IFileMetadataService
    {
        private readonly IFileMetadataRepository _fileMetadataRepository;
        private readonly ILogger<FileMetadataService> _logger;

        public FileMetadataService(
            IFileMetadataRepository fileMetadataRepository,
            ILogger<FileMetadataService> logger)
        {
            _fileMetadataRepository = fileMetadataRepository;
            _logger = logger;
        }

        public async Task<Entities.FileMetadata> CreateFileMetadataAsync(Guid fileId,
            string fileName,
            string originalName,
            long size,
            string contentType,
            Guid userId,
            string storagePath)
        {
            var fileMetadata = new Entities.FileMetadata(
                fileId, fileName, originalName, size, contentType, userId, storagePath, DateTime.UtcNow);

            var createdMetadata = await _fileMetadataRepository.AddAsync(fileMetadata);

            _logger.LogInformation("File metadata created: {FileId} for user {UserId}", fileId, userId);
            return createdMetadata;
        }

        public async Task<Entities.FileMetadata?> GetFileMetadataAsync(Guid fileId)
        {
            var metadata = await _fileMetadataRepository.GetByIdAsync(fileId);

            if (metadata != null)
            {
                await _fileMetadataRepository.UpdateLastAccessedAsync(fileId);
                _logger.LogDebug("File metadata retrieved: {FileId}", fileId);
            }

            return metadata;
        }

        public async Task<IEnumerable<Entities.FileMetadata>> GetUserFilesAsync(Guid userId)
        {
            var files = await _fileMetadataRepository.GetByUserIdAsync(userId);

            _logger.LogDebug("Retrieved {Count} files for user {UserId}", files.Count(), userId);
            return files;
        }

        public async Task<bool> DeleteFileMetadataAsync(Guid fileId)
        {
            var deleted = await _fileMetadataRepository.DeleteAsync(fileId);

            if (deleted)
            {
                _logger.LogInformation("File metadata deleted: {FileId}", fileId);
            }
            else
            {
                _logger.LogWarning("File metadata not found for deletion: {FileId}", fileId);
            }

            return deleted;
        }

        public async Task<bool> BelongsToUserAsync(Guid fileId, Guid userId)
        {
            var belongs = await _fileMetadataRepository.BelongsToUserAsync(fileId, userId);

            _logger.LogDebug("File {FileId} belongs to user {UserId}: {Belongs}", fileId, userId, belongs);
            return belongs;
        }

        public async Task UpdateLastAccessedAsync(Guid fileId)
        {
            await _fileMetadataRepository.UpdateLastAccessedAsync(fileId);
            _logger.LogDebug("Last accessed updated for file: {FileId}", fileId);
        }
    }
}
