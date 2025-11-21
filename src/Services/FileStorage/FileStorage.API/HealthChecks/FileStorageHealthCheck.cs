using FileStorage.Core.Interfaces.Repositories;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FileStorage.API.HealthChecks
{
    public class FileStorageHealthCheck : IHealthCheck
    {
        private readonly IFileStorageRepository _fileStorageRepository;
        private readonly ILogger<FileStorageHealthCheck> _logger;

        public FileStorageHealthCheck(IFileStorageRepository fileStorageRepository, ILogger<FileStorageHealthCheck> logger)
        {
            _fileStorageRepository = fileStorageRepository;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Test if storage is writable by creating a test file
                var testContent = "health_check_test";
                var testUserId = Guid.NewGuid();
                using var testStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testContent));

                var testFile = await _fileStorageRepository.SaveFileAsync(
                    testStream,
                    "health_check.txt",
                    "text/plain",
                    testContent.Length,
                    testUserId);

                // Test if we can read the file back
                var fileInfo = await _fileStorageRepository.GetFileAsync(testFile.Id);

                // Clean up test file
                await _fileStorageRepository.DeleteFileAsync(testFile.Id);

                if (fileInfo != null && fileInfo.Size == testContent.Length)
                {
                    return HealthCheckResult.Healthy("File storage is working correctly");
                }
                else
                {
                    return HealthCheckResult.Degraded("File storage test completed but with inconsistencies");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for file storage");
                return HealthCheckResult.Unhealthy("File storage health check failed", ex);
            }
        }
    }
}
