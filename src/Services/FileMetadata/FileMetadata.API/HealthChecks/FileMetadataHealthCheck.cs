using FileMetadata.Core.Interfaces.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FileMetadata.API.HealthChecks
{
    public class FileMetadataHealthCheck : IHealthCheck
    {
        private readonly IFileMetadataService _fileMetadataService;
        private readonly ILogger<FileMetadataHealthCheck> _logger;

        public FileMetadataHealthCheck(IFileMetadataService fileMetadataService, ILogger<FileMetadataHealthCheck> logger)
        {
            _fileMetadataService = fileMetadataService;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Test database connection by getting count (empty result is OK)
                var testUserId = Guid.NewGuid();
                var files = await _fileMetadataService.GetUserFilesAsync(testUserId);

                _logger.LogDebug("Health check: Database connection OK");
                return HealthCheckResult.Healthy("File Metadata Service is healthy");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for File Metadata Service");
                return HealthCheckResult.Unhealthy("File Metadata Service health check failed", ex);
            }
        }
    }
}
