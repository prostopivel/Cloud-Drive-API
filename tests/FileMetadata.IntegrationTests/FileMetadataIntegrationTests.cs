using FileMetadata.API;
using FileMetadata.IntegrationTests.DTOs;
using FileMetadata.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Shared.Messaging.Events;
using System.Net;
using System.Text.Json;
using Tests.Common;
using Xunit;

namespace FileMetadata.IntegrationTests
{
    public class FileMetadataIntegrationTests : BaseIntegrationTests<FileMetadataApiFactory, Program>
    {
        private static readonly Guid _testUserId = Guid.NewGuid();
        private readonly Func<Task> _resetState;

        public FileMetadataIntegrationTests(FileMetadataApiFactory factory)
            : base(factory, _testUserId.ToString())
        {
            _resetState = factory.ResetAsync;
        }

        public override Task InitializeAsync() => Task.CompletedTask;
        public override Task DisposeAsync() => _resetState();

        [Fact]
        public async Task GetUserFiles_WithNoFiles_ReturnsEmptyList()
        {
            // Act
            var response = await _client.GetAsync("/api/files");
            var content = await response.Content.ReadAsStringAsync();
            var files = JsonSerializer.Deserialize<List<FileMetadataResponse>>(content, _jsonOptions);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            files.Should().NotBeNull();
            files.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserFiles_WithFiles_ReturnsUserFiles()
        {
            // Arrange - Simulate file upload events
            var fileEvents = new[]
            {
            new FileUploadedEvent
            {
                FileId = Guid.NewGuid(),
                UserId = _testUserId,
                FileName = "file1.txt",
                OriginalName = "document1.txt",
                StoragePath = "/storage/file1.txt",
                FileSize = 1024,
                ContentType = "text/plain",
                UploadedAt = DateTime.UtcNow
            },
            new FileUploadedEvent
            {
                FileId = Guid.NewGuid(),
                UserId = _testUserId,
                FileName = "file2.jpg",
                OriginalName = "image2.jpg",
                StoragePath = "/storage/file2.jpg",
                FileSize = 2048,
                ContentType = "image/jpeg",
                UploadedAt = DateTime.UtcNow
            }
        };

            // Process events manually (simulating RabbitMQ consumer)
            foreach (var fileEvent in fileEvents)
            {
                await ProcessFileUploadEvent(fileEvent);
            }

            // Act
            var response = await _client.GetAsync("/api/files");
            var content = await response.Content.ReadAsStringAsync();
            var files = JsonSerializer.Deserialize<List<FileMetadataResponse>>(content, _jsonOptions);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            files.Should().NotBeNull();
            files.Should().HaveCount(2);
            files.Should().Contain(f => f.FileName == "document1.txt");
            files.Should().Contain(f => f.FileName == "image2.jpg");
        }

        [Fact]
        public async Task GetFileMetadata_WithExistingFile_ReturnsFileMetadata()
        {
            // Arrange
            var fileEvent = new FileUploadedEvent
            {
                FileId = Guid.NewGuid(),
                UserId = _testUserId,
                FileName = "testfile.txt",
                OriginalName = "testfile.txt",
                StoragePath = "/storage/testfile.txt",
                FileSize = 1024,
                ContentType = "text/plain",
                UploadedAt = DateTime.UtcNow
            };

            await ProcessFileUploadEvent(fileEvent);

            // Act
            var response = await _client.GetAsync($"/api/files/{fileEvent.FileId}");
            var content = await response.Content.ReadAsStringAsync();
            var fileMetadata = JsonSerializer.Deserialize<FileMetadataResponse>(content, _jsonOptions);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            fileMetadata.Should().NotBeNull();
            fileMetadata!.FileId.Should().Be(fileEvent.FileId);
            fileMetadata.FileName.Should().Be(fileEvent.OriginalName);
            fileMetadata.Size.Should().Be(fileEvent.FileSize);
            fileMetadata.ContentType.Should().Be(fileEvent.ContentType);
        }

        [Fact]
        public async Task GetFileMetadata_WithNonExistentFile_ReturnsNotFound()
        {
            // Arrange
            var nonExistentFileId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/files/{nonExistentFileId}");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            content.Should().Contain("not found");
        }

        [Fact]
        public async Task GetFileMetadata_WithFileFromDifferentUser_ReturnsForbidden()
        {
            // Arrange
            var otherUserId = Guid.NewGuid();
            var fileEvent = new FileUploadedEvent
            {
                FileId = Guid.NewGuid(),
                UserId = otherUserId, // Different user
                FileName = "otherfile.txt",
                OriginalName = "otherfile.txt",
                StoragePath = "/storage/otherfile.txt",
                FileSize = 1024,
                ContentType = "text/plain",
                UploadedAt = DateTime.UtcNow
            };

            await ProcessFileUploadEvent(fileEvent);

            // Act - Current user tries to access other user's file
            var response = await _client.GetAsync($"/api/files/{fileEvent.FileId}");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            content.Should().Contain("does not belong");
        }

        [Fact]
        public async Task DeleteFileMetadata_WithExistingFile_ReturnsSuccess()
        {
            // Arrange
            var fileEvent = new FileUploadedEvent
            {
                FileId = Guid.NewGuid(),
                UserId = _testUserId,
                FileName = "deletefile.txt",
                OriginalName = "deletefile.txt",
                StoragePath = "/storage/deletefile.txt",
                FileSize = 1024,
                ContentType = "text/plain",
                UploadedAt = DateTime.UtcNow
            };

            await ProcessFileUploadEvent(fileEvent);

            // Act
            var response = await _client.DeleteAsync($"/api/files/{fileEvent.FileId}");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().Contain("deleted successfully");

            // Verify file is actually deleted
            var getResponse = await _client.GetAsync($"/api/files/{fileEvent.FileId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteFileMetadata_WithNonExistentFile_ReturnsNotFound()
        {
            // Arrange
            var nonExistentFileId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/files/{nonExistentFileId}");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            content.Should().Contain("not found");
        }

        [Fact]
        public async Task DeleteFileMetadata_WithFileFromDifferentUser_ReturnsForbidden()
        {
            // Arrange
            var otherUserId = Guid.NewGuid();
            var fileEvent = new FileUploadedEvent
            {
                FileId = Guid.NewGuid(),
                UserId = otherUserId,
                FileName = "otherfile.txt",
                OriginalName = "otherfile.txt",
                StoragePath = "/storage/otherfile.txt",
                FileSize = 1024,
                ContentType = "text/plain",
                UploadedAt = DateTime.UtcNow
            };

            await ProcessFileUploadEvent(fileEvent);

            // Act
            var response = await _client.DeleteAsync($"/api/files/{fileEvent.FileId}");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            content.Should().Contain("does not belong");
        }

        [Fact]
        public async Task ValidateOwnership_WithUserFile_ReturnsTrue()
        {
            // Arrange
            var fileEvent = new FileUploadedEvent
            {
                FileId = Guid.NewGuid(),
                UserId = _testUserId,
                FileName = "userfile.txt",
                OriginalName = "userfile.txt",
                StoragePath = "/storage/userfile.txt",
                FileSize = 1024,
                ContentType = "text/plain",
                UploadedAt = DateTime.UtcNow
            };

            await ProcessFileUploadEvent(fileEvent);

            // Act
            var response = await _client.PostAsync($"/api/files/{fileEvent.FileId}/validate-ownership", null);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OwnershipValidationResponse>(content, _jsonOptions);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result!.BelongsToUser.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateOwnership_WithOtherUserFile_ReturnsFalse()
        {
            // Arrange
            var otherUserId = Guid.NewGuid();
            var fileEvent = new FileUploadedEvent
            {
                FileId = Guid.NewGuid(),
                UserId = otherUserId,
                FileName = "otherfile.txt",
                OriginalName = "otherfile.txt",
                StoragePath = "/storage/otherfile.txt",
                FileSize = 1024,
                ContentType = "text/plain",
                UploadedAt = DateTime.UtcNow
            };

            await ProcessFileUploadEvent(fileEvent);

            // Act
            var response = await _client.PostAsync($"/api/files/{fileEvent.FileId}/validate-ownership", null);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OwnershipValidationResponse>(content, _jsonOptions);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result!.BelongsToUser.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateOwnership_WithNonExistentFile_ReturnsFalse()
        {
            // Arrange
            var nonExistentFileId = Guid.NewGuid();

            // Act
            var response = await _client.PostAsync($"/api/files/{nonExistentFileId}/validate-ownership", null);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OwnershipValidationResponse>(content, _jsonOptions);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result!.BelongsToUser.Should().BeFalse();
        }

        [Fact]
        public async Task FileUploadEvent_ProcessedCorrectly_CreatesMetadata()
        {
            // Arrange
            var fileEvent = new FileUploadedEvent
            {
                FileId = Guid.NewGuid(),
                UserId = _testUserId,
                FileName = "storagefile.txt",
                OriginalName = "originalfile.txt",
                StoragePath = "/storage/storagefile.txt",
                FileSize = 5120,
                ContentType = "text/plain",
                UploadedAt = DateTime.UtcNow
            };

            // Act - Simulate RabbitMQ message processing
            await ProcessFileUploadEvent(fileEvent);

            // Assert - Verify metadata was created
            var response = await _client.GetAsync($"/api/files/{fileEvent.FileId}");
            var content = await response.Content.ReadAsStringAsync();
            var fileMetadata = JsonSerializer.Deserialize<FileMetadataResponse>(content, _jsonOptions);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            fileMetadata.Should().NotBeNull();
            fileMetadata!.FileId.Should().Be(fileEvent.FileId);
            fileMetadata.FileName.Should().Be(fileEvent.OriginalName);
            fileMetadata.Size.Should().Be(fileEvent.FileSize);
            fileMetadata.ContentType.Should().Be(fileEvent.ContentType);
        }

        [Fact]
        public async Task ProcessFileUploadEvent_CreatesMetadata()
        {
            // Arrange
            var fileEvent = new FileUploadedEvent
            {
                FileId = Guid.NewGuid(),
                UserId = _testUserId,
                FileName = "storagefile.txt",
                OriginalName = "originalfile.txt",
                StoragePath = "/storage/storagefile.txt",
                FileSize = 5120,
                ContentType = "text/plain",
                UploadedAt = DateTime.UtcNow
            };

            // Act - Simulate message bus event
            _factory.MessageBus.Publish(fileEvent, "file_events");

            // Assert - Verify metadata was created
            await WaitForCondition(async () =>
            {
                var response = await _client.GetAsync($"/api/files/{fileEvent.FileId}");
                return response.StatusCode == HttpStatusCode.OK;
            }, TimeSpan.FromSeconds(5));

            var response = await _client.GetAsync($"/api/files/{fileEvent.FileId}");
            var content = await response.Content.ReadAsStringAsync();
            var fileMetadata = JsonSerializer.Deserialize<FileMetadataResponse>(content, _jsonOptions);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            fileMetadata.Should().NotBeNull();
            fileMetadata!.FileId.Should().Be(fileEvent.FileId);
            fileMetadata.FileName.Should().Be(fileEvent.OriginalName);
            fileMetadata.Size.Should().Be(fileEvent.FileSize);
            fileMetadata.ContentType.Should().Be(fileEvent.ContentType);
        }

        private async Task ProcessFileUploadEvent(FileUploadedEvent fileEvent)
        {
            using var scope = _factory.Services.CreateScope();
            var fileMetadataService = scope.ServiceProvider.GetRequiredService<Core.Interfaces.Services.IFileMetadataService>();

            await fileMetadataService.CreateFileMetadataAsync(
                fileEvent.FileId,
                fileEvent.FileName,
                fileEvent.OriginalName,
                fileEvent.FileSize,
                fileEvent.ContentType,
                fileEvent.UserId,
                fileEvent.StoragePath);
        }

        private static async Task WaitForCondition(Func<Task<bool>> condition,
            TimeSpan timeout)
        {
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < timeout)
            {
                if (await condition())
                    return;

                await Task.Delay(100);
            }
            throw new TimeoutException("Condition not met within timeout");
        }
    }
}
