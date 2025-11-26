using ApiGateway.DTOs;
using ApiGateway.Interfaces;
using Shared.Common.Exceptions;

namespace ApiGateway.Services
{
    public class FilesService : IFilesService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FilesService> _logger;

        public FilesService(IHttpClientFactory httpClientFactory,
            ILogger<FilesService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task DeleteFileAsync(Guid fileId, Guid userId)
        {
            // First validate ownership via File Metadata Service
            var metadataClient = _httpClientFactory.CreateClient("FileMetadataService");
            var validationResponse = await metadataClient.PostAsync(
                $"/api/files/{fileId}/validate-ownership",
                null);

            if (!validationResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Download denied for file {FileId} by user {UserId}", fileId, userId);
                throw new AppException("File access denied", validationResponse.StatusCode);
            }

            var validationResult = await validationResponse.Content.ReadFromJsonAsync<OwnershipValidationResponse>();
            if (!validationResult?.BelongsToUser == true)
            {
                _logger.LogWarning("Download ownership validation failed for file {FileId} by user {UserId}", fileId, userId);
                throw new ForbidException("File does not belong to user");
            }

            // If ownership validated, redirect to download endpoint (handled by YARP)
            _logger.LogInformation("Download authorized for file {FileId} by user {UserId}", fileId, userId);
        }

        public async Task DownloadFileAsync(Guid fileId, Guid userId)
        {
            // Delete from File Metadata Service
            var metadataClient = _httpClientFactory.CreateClient("FileMetadataService");
            var metadataResponse = await metadataClient.DeleteAsync($"/api/files/{fileId}");

            if (!metadataResponse.IsSuccessStatusCode)
            {
                throw new AppException("Error deleting file metadata", metadataResponse.StatusCode);
            }

            // Delete from File Storage Service
            var storageClient = _httpClientFactory.CreateClient("FileStorageService");
            var storageResponse = await storageClient.DeleteAsync($"/api/files/{fileId}");

            if (!storageResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("File metadata deleted but storage deletion failed for {FileId}", fileId);
                // Continue anyway as metadata is already deleted
            }

            _logger.LogInformation("File deleted successfully: {FileId} by user {UserId}", fileId, userId);
        }
    }
}
