using Bogus;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Text;
using Xunit;

namespace FileStorage.LoadTests
{
    public class FileStorageLoadTests
    {
        private readonly string _baseUrl = "http://localhost:5003";
        private readonly Faker _faker = new();
        private readonly HttpClient _httpClient = new();

        [Fact]
        public void FileStorageService_LoadTest()
        {
            // Upload scenario
            var uploadScenario = Scenario.Create("file_upload", async context =>
            {
                var fileContent = GenerateTestFileContent(1024); // 1KB file
                var fileName = _faker.System.FileName("txt");

                using var content = new MultipartFormDataContent
                {
                    { new ByteArrayContent(fileContent), "file", fileName }
                };

                var request = Http.CreateRequest("POST", $"{_baseUrl}/api/files/upload")
                    .WithHeader("userId", Guid.NewGuid().ToString())
                    .WithBody(content);

                var response = await Http.Send(_httpClient, request);
                return response;
            })
                .WithWarmUpDuration(TimeSpan.FromSeconds(10))
                .WithLoadSimulations(
                    Simulation.Inject(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30)) // 5 RPS for 30 seconds
                );

            // Download scenario
            var downloadScenario = Scenario.Create("file_download", async context =>
            {
                // First upload a file to download
                var fileId = await UploadTestFile();

                var request = Http.CreateRequest("GET", $"{_baseUrl}/api/files/download/{fileId}")
                    .WithHeader("userId", Guid.NewGuid().ToString());

                var response = await Http.Send(_httpClient, request);
                return response;
            })
                .WithWarmUpDuration(TimeSpan.FromSeconds(10))
                .WithLoadSimulations(
                    Simulation.Inject(10, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30)) // 10 RPS for 30 seconds
                );

            // Mixed workload scenario
            var mixedScenario = Scenario.Create("file_mixed_workload", async context =>
            {
                var operation = context.Random.Next(0, 3);

                return operation switch
                {
                    0 => await UploadOperation(),
                    1 => await DownloadOperation(),
                    2 => await InfoOperation(),
                    _ => await UploadOperation()
                };
            })
                .WithWarmUpDuration(TimeSpan.FromSeconds(10))
                .WithLoadSimulations(
                    Simulation.Inject(15, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60)) // 15 RPS for 1 minute
                );

            // Run all scenarios
            NBomberRunner
                .RegisterScenarios(uploadScenario, downloadScenario, mixedScenario)
                .Run();
        }

        [Fact]
        public void FileStorageService_StressTest()
        {
            var stressScenario = Scenario.Create("file_stress", async context =>
            {
                var fileSize = context.Random.Next(1024, 1024 * 1024); // 1KB to 1MB
                var fileContent = GenerateTestFileContent(fileSize);
                var fileName = _faker.System.FileName("txt");

                using var content = new MultipartFormDataContent
                {
                    { new ByteArrayContent(fileContent), "file", fileName }
                };

                var request = Http.CreateRequest("POST", $"{_baseUrl}/api/files/upload")
                    .WithHeader("userId", Guid.NewGuid().ToString())
                    .WithBody(content);

                var response = await Http.Send(_httpClient, request);
                return response;
            })
                .WithWarmUpDuration(TimeSpan.FromSeconds(10))
                .WithLoadSimulations(
                    Simulation.Inject(20, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(120)) // 20 RPS for 2 minutes
                );

            NBomberRunner
                .RegisterScenarios(stressScenario)
                .Run();
        }

        [Fact]
        public void FileStorageService_ConcurrentUploadTest()
        {
            var concurrentScenario = Scenario.Create("file_concurrent_upload", async context =>
            {
                var fileSize = 100 * 1024; // 100KB files
                var fileContent = GenerateTestFileContent(fileSize);
                var fileName = _faker.System.FileName("txt");

                using var content = new MultipartFormDataContent
                {
                    { new ByteArrayContent(fileContent), "file", fileName }
                };

                var request = Http.CreateRequest("POST", $"{_baseUrl}/api/files/upload")
                    .WithHeader("userId", Guid.NewGuid().ToString())
                    .WithBody(content);

                var response = await Http.Send(_httpClient, request);
                return response;
            })
                .WithWarmUpDuration(TimeSpan.FromSeconds(10))
                .WithLoadSimulations(
                    Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(60)),
                    Simulation.RampingInject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
                );

            NBomberRunner
                .RegisterScenarios(concurrentScenario)
                .Run();
        }

        private byte[] GenerateTestFileContent(int sizeInBytes)
        {
            var content = new StringBuilder();
            var line = _faker.Lorem.Sentence();

            while (content.Length < sizeInBytes)
            {
                content.AppendLine(line);
            }

            return Encoding.UTF8.GetBytes(content.ToString()[..sizeInBytes]);
        }

        private async Task<Guid> UploadTestFile()
        {
            var fileContent = GenerateTestFileContent(1024);
            var fileName = _faker.System.FileName("txt");

            using var content = new MultipartFormDataContent
            {
                { new ByteArrayContent(fileContent), "file", fileName }
            };

            var request = Http.CreateRequest("POST", $"{_baseUrl}/api/files/upload")
                .WithHeader("userId", Guid.NewGuid().ToString())
                .WithBody(content);

            var response = await Http.Send(_httpClient, request);

            if (response.IsError)
            {
                throw new Exception("Failed to upload test file");
            }

            // Parse response to get file ID (in real scenario, you'd deserialize JSON)
            return Guid.NewGuid(); // Simplified for example
        }

        private async Task<IResponse> UploadOperation()
        {
            var fileContent = GenerateTestFileContent(10 * 1024); // 10KB
            var fileName = _faker.System.FileName("txt");

            using var content = new MultipartFormDataContent
            {
                { new ByteArrayContent(fileContent), "file", fileName }
            };

            var request = Http.CreateRequest("POST", $"{_baseUrl}/api/files/upload")
                .WithHeader("userId", Guid.NewGuid().ToString())
                .WithBody(content);

            return await Http.Send(_httpClient, request);
        }

        private async Task<IResponse> DownloadOperation()
        {
            var request = Http.CreateRequest("GET", $"{_baseUrl}/api/files/download/{Guid.NewGuid()}")
                .WithHeader("userId", Guid.NewGuid().ToString());

            return await Http.Send(_httpClient, request);
        }

        private async Task<IResponse> InfoOperation()
        {
            var request = Http.CreateRequest("GET", $"{_baseUrl}/api/files/{Guid.NewGuid()}/info")
                .WithHeader("userId", Guid.NewGuid().ToString());

            return await Http.Send(_httpClient, request);
        }
    }
}
