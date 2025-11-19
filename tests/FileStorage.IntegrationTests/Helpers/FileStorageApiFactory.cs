using FileStorage.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shared.Common.Models;
using Tests.Common;

namespace FileStorage.IntegrationTests.Helpers
{
    public class FileStorageApiFactory : BaseApiFactory<Program>
    {
        private readonly string _testStoragePath;

        public FileStorageApiFactory()
            : base()
        {
            _testStoragePath = Path.Combine(Path.GetTempPath(), $"filestorage_test_{Guid.NewGuid()}");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                // Override storage settings
                services.Configure<FileStorageSettings>(options =>
                {
                    options.StoragePath = _testStoragePath;
                    options.MaxFileSize = 10 * 1024 * 1024; // 10MB for tests
                    options.AllowedExtensions = [".txt", ".pdf", ".jpg", ".png", ".zip"];
                });
            });
        }

        public override async Task InitializeAsync()
        {
            // Ensure clean test directory
            if (Directory.Exists(_testStoragePath))
            {
                Directory.Delete(_testStoragePath, true);
            }
            Directory.CreateDirectory(_testStoragePath);

            await Task.CompletedTask;
        }

        public override async Task DisposeAsync()
        {
            // Cleanup test directory
            if (Directory.Exists(_testStoragePath))
            {
                try
                {
                    Directory.Delete(_testStoragePath, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            await Task.CompletedTask;
        }

        protected override bool CanConnectToExistingContainers()
        {
            return false;
        }
    }
}
