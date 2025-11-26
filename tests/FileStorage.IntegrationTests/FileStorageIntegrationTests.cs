using FileStorage.API;
using FileStorage.API.DTOs;
using FileStorage.IntegrationTests.Helpers;
using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.Json;
using Tests.Common;
using Xunit;

namespace FileStorage.IntegrationTests
{
    public class FileStorageIntegrationTests : BaseIntegrationTests<FileStorageApiFactory, Program>
    {
        private static readonly Guid _testUserId = Guid.NewGuid();

        public FileStorageIntegrationTests(FileStorageApiFactory factory)
             : base(factory, _testUserId.ToString())
        { }

        public override Task InitializeAsync() => Task.CompletedTask;
        public override Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task UploadFile_WithValidFile_ReturnsSuccess()
        {
            // Arrange
            var fileContent = "This is a test file content";
            using var content = new MultipartFormDataContent
            {
                { new ByteArrayContent(Encoding.UTF8.GetBytes(fileContent)), "file", "testfile.txt" }
            };

            // Act
            var response = await _client.PostAsync("/api/files/upload", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<FileUploadResponse>(responseContent, _jsonOptions);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result!.FileId.Should().NotBeEmpty();
            result.FileName.Should().Be("testfile.txt");
            result.Size.Should().Be(fileContent.Length);
        }

        [Fact]
        public async Task UploadFile_WithLargeFile_ReturnsBadRequest()
        {
            // Arrange
            var largeContent = new byte[11 * 1024 * 1024]; // 11MB - exceeds 10MB limit
            using var content = new MultipartFormDataContent
            {
                { new ByteArrayContent(largeContent), "file", "largefile.bin" }
            };

            // Act
            var response = await _client.PostAsync("/api/files/upload", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            responseContent.Should().Contain("File size exceeds");
        }

        [Fact]
        public async Task UploadFile_WithInvalidExtension_ReturnsBadRequest()
        {
            // Arrange
            var fileContent = "Invalid extension file";
            using var content = new MultipartFormDataContent
            {
                { new ByteArrayContent(Encoding.UTF8.GetBytes(fileContent)), "file", "testfile.exe" }
            };

            // Act
            var response = await _client.PostAsync("/api/files/upload", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            responseContent.Should().Contain("File extension .exe is not allowed");
        }

        [Fact]
        public async Task DownloadFile_WithExistingFile_ReturnsFile()
        {
            // Arrange - Upload a file first
            var fileContent = "Download test content";
            var uploadResponse = await UploadTestFile("download_test.txt", fileContent);
            var uploadResult = JsonSerializer.Deserialize<FileUploadResponse>(
                await uploadResponse.Content.ReadAsStringAsync(), _jsonOptions);

            // Act
            var downloadResponse = await _client.GetAsync($"/api/files/download/{uploadResult!.FileId}");
            var downloadedContent = await downloadResponse.Content.ReadAsStringAsync();

            // Assert
            downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            downloadResponse.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
            downloadedContent.Should().Be(fileContent);
        }

        [Fact]
        public async Task DownloadFile_WithNonExistentFile_ReturnsNotFound()
        {
            // Arrange
            var nonExistentFileId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/files/download/{nonExistentFileId}");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            responseContent.Should().Contain("not found");
        }

        [Fact]
        public async Task GetFileInfo_WithExistingFile_ReturnsFileInfo()
        {
            // Arrange
            var fileContent = "File info test content";
            var uploadResponse = await UploadTestFile("info_test.txt", fileContent);
            var uploadResult = JsonSerializer.Deserialize<FileUploadResponse>(
                await uploadResponse.Content.ReadAsStringAsync(), _jsonOptions);

            // Act
            var infoResponse = await _client.GetAsync($"/api/files/{uploadResult!.FileId}/info");
            var infoContent = await infoResponse.Content.ReadAsStringAsync();
            var infoResult = JsonSerializer.Deserialize<FileInfoResponse>(infoContent, _jsonOptions);

            // Assert
            infoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            infoResult.Should().NotBeNull();
            infoResult!.FileId.Should().Be(uploadResult.FileId);
            infoResult.Size.Should().Be(fileContent.Length);
            infoResult.ContentType.Should().Be("text/plain");
        }

        [Fact]
        public async Task DeleteFile_WithExistingFile_ReturnsSuccess()
        {
            // Arrange
            var fileContent = "Delete test content";
            var uploadResponse = await UploadTestFile("delete_test.txt", fileContent);
            var uploadResult = JsonSerializer.Deserialize<FileUploadResponse>(
                await uploadResponse.Content.ReadAsStringAsync(), _jsonOptions);

            // Act
            var deleteResponse = await _client.DeleteAsync($"/api/files/{uploadResult!.FileId}");
            var deleteContent = await deleteResponse.Content.ReadAsStringAsync();

            // Assert
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            deleteContent.Should().Contain("deleted successfully");

            // Verify file is actually deleted
            var infoResponse = await _client.GetAsync($"/api/files/{uploadResult.FileId}/info");
            infoResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteFile_WithNonExistentFile_ReturnsNotFound()
        {
            // Arrange
            var nonExistentFileId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/files/{nonExistentFileId}");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            responseContent.Should().Contain("not found");
        }

        [Fact]
        public async Task UploadMultipleFiles_WithDifferentUsers_StoresSeparately()
        {
            // Arrange
            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            var client1 = _factory.CreateClientWithUserId(user1.ToString());
            var client2 = _factory.CreateClientWithUserId(user2.ToString());

            // Act
            var file1Response = await UploadTestFile(client1, "user1_file.txt", "User 1 content");
            var file2Response = await UploadTestFile(client2, "user2_file.txt", "User 2 content");

            var file1Result = JsonSerializer.Deserialize<FileUploadResponse>(
                await file1Response.Content.ReadAsStringAsync(), _jsonOptions);

            var file2Result = JsonSerializer.Deserialize<FileUploadResponse>(
                await file2Response.Content.ReadAsStringAsync(), _jsonOptions);

            // Assert - Both uploads should succeed
            file1Response.StatusCode.Should().Be(HttpStatusCode.OK);
            file2Response.StatusCode.Should().Be(HttpStatusCode.OK);

            file1Result!.FileId.Should().NotBe(file2Result!.FileId);
        }

        [Fact]
        public async Task HealthCheck_ReturnsHealthy()
        {
            // Act
            var response = await _client.GetAsync("/health");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            responseContent.Should().Contain("Healthy");
        }

        private async Task<HttpResponseMessage> UploadTestFile(string fileName,
            string content)
        {
            return await UploadTestFile(_client, fileName, content);
        }

        private async Task<HttpResponseMessage> UploadTestFile(HttpClient client,
            string fileName,
            string content)
        {
            using var formContent = new MultipartFormDataContent
            {
                { new ByteArrayContent(Encoding.UTF8.GetBytes(content)), "file", fileName }
            };
            return await client.PostAsync("/api/files/upload", formContent);
        }
    }
}
