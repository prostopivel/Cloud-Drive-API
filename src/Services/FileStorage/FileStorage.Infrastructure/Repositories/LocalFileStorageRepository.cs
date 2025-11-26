using FileStorage.Core.Entities;
using FileStorage.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Common.Models;

namespace FileStorage.Infrastructure.Repositories
{
    public class LocalFileStorageRepository : IFileStorageRepository
    {
        private readonly string _storagePath;
        private readonly ILogger<LocalFileStorageRepository> _logger;

        public LocalFileStorageRepository(IOptions<FileStorageSettings> settings,
            ILogger<LocalFileStorageRepository> logger)
        {
            _storagePath = settings.Value.StoragePath;
            _logger = logger;

            // Ensure storage directory exists
            Directory.CreateDirectory(_storagePath);
        }

        public async Task<StoredFile> SaveFileAsync(Stream fileStream,
            string fileName,
            string contentType,
            long fileSize,
            Guid userId,
            CancellationToken token = default)
        {
            var fileId = Guid.NewGuid();
            var fileExtension = Path.GetExtension(fileName);
            var storageFileName = $"{fileId}{fileExtension}";
            var storageFilePath = Path.Combine(_storagePath, storageFileName);

            using (var file = new FileStream(storageFilePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(file, token);
            }

            var storedFile = new StoredFile
            {
                Id = fileId,
                FileName = storageFileName,
                OriginalName = fileName,
                StoragePath = storageFilePath,
                ContentType = contentType,
                Size = fileSize,
                UploadedAt = DateTime.UtcNow,
                UserId = userId
            };

            _logger.LogInformation("File saved: {FileName} -> {StoragePath}, Size: {Size} bytes",
                fileName, storageFilePath, fileSize);

            return storedFile;
        }

        public Task<StoredFile?> GetFileAsync(Guid fileId,
            CancellationToken token = default)
        {
            var files = Directory.GetFiles(_storagePath, $"{fileId}.*");
            if (files.Length == 0)
            {
                return Task.FromResult<StoredFile?>(null);
            }

            var filePath = files[0];
            var fileInfo = new FileInfo(filePath);

            var storedFile = new StoredFile
            {
                Id = fileId,
                FileName = Path.GetFileName(filePath),
                StoragePath = filePath,
                ContentType = GetContentType(filePath),
                Size = fileInfo.Length,
                UploadedAt = fileInfo.CreationTimeUtc
            };

            return Task.FromResult<StoredFile?>(storedFile);
        }

        public Task<bool> DeleteFileAsync(Guid fileId,
            CancellationToken token = default)
        {
            var files = Directory.GetFiles(_storagePath, $"{fileId}.*");
            if (files.Length == 0)
            {
                return Task.FromResult(false);
            }

            File.Delete(files[0]);
            _logger.LogInformation("File deleted: {FileId}", fileId);
            return Task.FromResult(true);
        }

        public Task<Stream> GetFileStreamAsync(Guid fileId,
            CancellationToken token = default)
        {
            var files = Directory.GetFiles(_storagePath, $"{fileId}.*");
            if (files.Length == 0)
            {
                throw new FileNotFoundException($"File with id {fileId} not found");
            }

            var fileStream = new FileStream(files[0], FileMode.Open, FileAccess.Read);
            return Task.FromResult<Stream>(fileStream);
        }

        private static string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".txt" => "text/plain",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };
        }
    }
}
