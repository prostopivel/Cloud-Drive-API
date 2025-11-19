using FileStorage.Core.Entities;
using FileStorage.Core.Exceptions;
using FileStorage.Core.Interfaces.Repositories;
using FileStorage.Core.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Common.Exceptions;
using Shared.Common.Models;

namespace FileStorage.Core.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IFileStorageRepository _fileStorageRepository;
        private readonly FileStorageSettings _settings;
        private readonly ILogger<FileStorageService> _logger;

        public FileStorageService(
            IFileStorageRepository fileStorageRepository,
            IOptions<FileStorageSettings> settings,
            ILogger<FileStorageService> logger)
        {
            _fileStorageRepository = fileStorageRepository;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<StoredFile> UploadFileAsync(IFormFile file,
            Guid? userId = null,
            CancellationToken token = default)
        {
            ValidateFile(file);

            await using var fileStream = file.OpenReadStream();
            var storedFile = await _fileStorageRepository.SaveFileAsync(
                fileStream,
                file.FileName,
                file.ContentType,
                file.Length,
                userId,
                token);

            _logger.LogInformation("File uploaded successfully: {FileId} ({FileName}, {Size} bytes)",
                storedFile.Id, storedFile.FileName, storedFile.Size);

            return storedFile;
        }

        public async Task<FileDownloadResult> DownloadFileAsync(Guid fileId,
            CancellationToken token = default)
        {
            var fileInfo = await _fileStorageRepository.GetFileAsync(fileId, token);
            if (fileInfo == null)
            {
                _logger.LogWarning("File not found for download: {FileId}", fileId);
                throw new NotFoundException($"File with id {fileId} not found");
            }

            var fileStream = await _fileStorageRepository.GetFileStreamAsync(fileId, token);

            _logger.LogInformation("File prepared for download: {FileId} ({FileName})",
                fileId, fileInfo.FileName);

            return new FileDownloadResult(fileStream, fileInfo.FileName, fileInfo.ContentType);
        }

        public async Task DeleteFileAsync(Guid fileId,
            CancellationToken token = default)
        {
            var fileInfo = await _fileStorageRepository.GetFileAsync(fileId, token);
            if (fileInfo == null)
            {
                _logger.LogWarning("File not found for deletion: {FileId}", fileId);
                throw new NotFoundException("File not found");
            }

            var deleted = await _fileStorageRepository.DeleteFileAsync(fileId, token);

            if (deleted)
            {
                _logger.LogInformation("File deleted successfully: {FileId} ({FileName})",
                    fileId, fileInfo.FileName);
            }
        }

        public async Task<StoredFile> GetFileInfoAsync(Guid fileId,
            CancellationToken token = default)
        {
            var fileInfo = await _fileStorageRepository.GetFileAsync(fileId, token);

            if (fileInfo == null)
            {
                _logger.LogDebug("File info not found: {FileId}", fileId);
                throw new NotFoundException("File not found");
            }

            return fileInfo;
        }

        private void ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new InvalidFileException("File is empty");
            }

            if (file.Length > _settings.MaxFileSize)
            {
                throw new InvalidFileException($"File size exceeds maximum allowed size of {_settings.MaxFileSize} bytes");
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_settings.AllowedExtensions.Contains(fileExtension))
            {
                throw new InvalidFileException($"File extension {fileExtension} is not allowed. Allowed extensions: {string.Join(", ", _settings.AllowedExtensions)}");
            }

            _logger.LogDebug("File validation passed: {FileName} ({Size} bytes, {ContentType})",
                file.FileName, file.Length, file.ContentType);
        }
    }
}
