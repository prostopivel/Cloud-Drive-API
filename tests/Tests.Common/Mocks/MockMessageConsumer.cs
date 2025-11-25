using FileMetadata.Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Messaging.Events;
using Shared.Messaging.Interfaces;

namespace Tests.Common.Mocks
{
    public class MockMessageConsumer : IMessageConsumer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMessageBus _messageBus;
        private readonly ILogger<MockMessageConsumer> _logger;

        public MockMessageConsumer(
            IServiceProvider serviceProvider,
            IMessageBus messageBus,
            ILogger<MockMessageConsumer> logger)
        {
            _serviceProvider = serviceProvider;
            _messageBus = messageBus;
            _logger = logger;
        }

        public void StartConsuming()
        {
            _messageBus.Subscribe<FileUploadedEvent>("file_events", "file_metadata_queue", async fileEvent =>
            {
                await ProcessFileUploadEventAsync(fileEvent);
            });

            _logger.LogInformation("Mock message consumer started successfully");
        }

        public void StopConsuming()
        {
            _logger.LogInformation("Mock message consumer stopped");
        }

        private async Task ProcessFileUploadEventAsync(FileUploadedEvent fileEvent)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var fileMetadataService = scope.ServiceProvider.GetRequiredService<IFileMetadataService>();

                await fileMetadataService.CreateFileMetadataAsync(
                    fileEvent.FileId,
                    fileEvent.FileName,
                    fileEvent.OriginalName,
                    fileEvent.FileSize,
                    fileEvent.ContentType,
                    fileEvent.UserId,
                    fileEvent.StoragePath);

                _logger.LogInformation("Processed file upload event: {FileId} for user {UserId}",
                    fileEvent.FileId, fileEvent.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file upload event: {FileId}", fileEvent.FileId);
                throw;
            }
        }
    }
}
