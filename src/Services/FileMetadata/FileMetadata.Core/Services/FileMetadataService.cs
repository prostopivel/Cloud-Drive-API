using FileMetadata.Core.Constants;
using FileMetadata.Core.Interfaces.Repositories;
using FileMetadata.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Shared.Caching.Interfaces;
using Shared.Common.Exceptions;
using System.Collections;

namespace FileMetadata.Core.Services
{
    public class FileMetadataService : IFileMetadataService
    {
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

        private readonly IFileMetadataRepository _fileMetadataRepository;
        private readonly ICacheService _cacheService;
        private readonly ILogger<FileMetadataService> _logger;

        public FileMetadataService(
            IFileMetadataRepository fileMetadataRepository,
            ICacheService cacheService,
            ILogger<FileMetadataService> logger)
        {
            _fileMetadataRepository = fileMetadataRepository;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Entities.FileMetadata> CreateFileMetadataAsync(Guid fileId,
            string fileName,
            string originalName,
            long size,
            string contentType,
            Guid userId,
            string storagePath,
            CancellationToken token = default)
        {
            var fileMetadata = new Entities.FileMetadata(
                fileId, fileName, originalName, size, contentType, userId, storagePath, DateTime.UtcNow);

            var exists = await _fileMetadataRepository.ExistsAsync(fileId, token: token);
            if (exists)
            {
                throw new ConflictException($"File wuth id {fileId} already exists");
            }

            var createdMetadata = await _fileMetadataRepository.AddAsync(fileMetadata, token: token);

            await InvalidateUserFilesCache(userId, token: token);

            _logger.LogInformation("File metadata created: {FileId} for user {UserId}", fileId, userId);
            return createdMetadata;
        }

        public async Task<Entities.FileMetadata> GetFileMetadataAsync(Guid fileId,
            Guid userId,
            CancellationToken token = default)
        {
            var cacheKey = $"{CacheKeys.FILE_BY_ID}:{fileId}";

            var metadata = await _cacheService.GetAsync<Entities.FileMetadata>(
                cacheKey, token: token);
            if (metadata != null)
            {
                _logger.LogDebug("File metadata retrieved from cache: {FileId}", fileId);

                if (metadata.UserId != userId)
                {
                    throw new ForbidException("File does not belong to user");
                }

                await _fileMetadataRepository.UpdateLastAccessedAsync(fileId, token: token);
                return metadata;
            }

            await ExistsFileMetadataAsync(fileId, token: token);
            await BelongsToUserAsync(fileId, userId, token: token);

            metadata = await _fileMetadataRepository.GetByIdAsync(fileId, token: token);

            if (metadata != null)
            {
                await _cacheService.SetAsync(cacheKey, metadata,
                    CacheExpiration, token: token);
            }

            await _fileMetadataRepository.UpdateLastAccessedAsync(fileId, token: token);
            _logger.LogDebug("File metadata retrieved from database: {FileId}", fileId);

            return metadata!;
        }

        public async Task<IEnumerable<Entities.FileMetadata>> GetUserFilesAsync(Guid userId,
            CancellationToken token = default)
        {
            var cacheKey = $"{CacheKeys.FILES_BY_USER_ID}:{userId}";

            var files = await _cacheService.GetAsync<IEnumerable<Entities.FileMetadata>>(
                cacheKey, token: token);
            if (files != null)
            {
                _logger.LogDebug("User files retrieved from cache for user {UserId}", userId);
                return files;
            }

            files = await _fileMetadataRepository.GetByUserIdAsync(userId, token: token);

            if (files != null && files.Any())
            {
                await _cacheService.SetAsync(cacheKey, files,
                    CacheExpiration, token: token);
            }

            _logger.LogDebug("Retrieved {Count} files for user {UserId} from database", files?.Count() ?? 0, userId);
            return files ?? [];
        }

        public async Task DeleteFileMetadataAsync(Guid fileId,
            Guid userId,
            CancellationToken token = default)
        {
            var exists = await _fileMetadataRepository.ExistsAsync(fileId, token: token);
            if (!exists)
            {
                throw new NotFoundException($"File with id {fileId} not found");
            }

            var metadata = await _fileMetadataRepository.GetByIdAsync(fileId, token: token);

            if (metadata.UserId != userId)
            {
                throw new ForbidException("File does not belong to user");
            }

            await _fileMetadataRepository.DeleteAsync(fileId, token: token);

            await InvalidateFileCache(fileId, userId, token: token);

            _logger.LogInformation("File metadata deleted: {FileId}", fileId);
        }

        public async Task BelongsToUserAsync(Guid fileId,
            Guid userId,
            CancellationToken token = default)
        {
            var cacheKey = $"{CacheKeys.USER_BY_FILE_ID}:{fileId}";

            var cachedResult = await _cacheService.GetAsync<bool?>(
                cacheKey, token: token);
            if (cachedResult.HasValue)
            {
                if (!cachedResult.Value)
                {
                    throw new ForbidException("File does not belong to user");
                }
                _logger.LogDebug("File {FileId} belongs to user {UserId}: {Belongs}", fileId, userId, cachedResult.Value);
                return;
            }

            var belongs = await _fileMetadataRepository.BelongsToUserAsync(fileId, userId, token: token);

            await _cacheService.SetAsync(cacheKey, belongs,
                TimeSpan.FromMinutes(5), token: token);

            if (!belongs)
            {
                throw new ForbidException("File does not belong to user");
            }

            _logger.LogDebug("File {FileId} belongs to user {UserId}: {Belongs}", fileId, userId, belongs);
        }

        public async Task ExistsFileMetadataAsync(Guid fileId,
            CancellationToken token = default)
        {
            var cacheKey = $"{CacheKeys.FILE_EXISTS}:{fileId}";

            var cachedResult = await _cacheService.GetAsync<bool?>(
                cacheKey, token: token);
            if (cachedResult.HasValue)
            {
                if (!cachedResult.Value)
                {
                    throw new NotFoundException($"File with id {fileId} not found");
                }
                return;
            }

            var exists = await _fileMetadataRepository.ExistsAsync(fileId, token: token);

            await _cacheService.SetAsync(cacheKey, exists,
                TimeSpan.FromMinutes(5), token: token);

            if (!exists)
            {
                throw new NotFoundException($"File with id {fileId} not found");
            }
        }

        private async Task InvalidateFileCache(Guid fileId,
            Guid userId,
            CancellationToken token = default)
        {
            var tasks = new List<Task>
            {
                _cacheService.RemoveAsync($"{CacheKeys.FILE_BY_ID}:{fileId}", token: token),
                _cacheService.RemoveAsync($"{CacheKeys.USER_BY_FILE_ID}:{fileId}", token: token),
                _cacheService.RemoveAsync($"{CacheKeys.FILE_EXISTS}:{fileId}", token: token),
                _cacheService.RemoveAsync($"{CacheKeys.FILES_BY_USER_ID}:{userId}", token: token),
            };

            await Task.WhenAll(tasks);
            _logger.LogDebug("File cache invalidated for file {FileId}", fileId);
        }

        private async Task InvalidateUserFilesCache(Guid userId,
            CancellationToken token = default)
        {
            await _cacheService.RemoveAsync($"{CacheKeys.FILES_BY_USER_ID}:{userId}", token: token);
            _logger.LogDebug("User files cache invalidated for user {UserId}", userId);
        }
    }
}
