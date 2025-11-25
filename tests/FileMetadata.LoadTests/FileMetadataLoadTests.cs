using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;

namespace FileMetadata.LoadTests
{
    public class FileMetadataLoadTests
    {
        private readonly string _baseUrl = "http://localhost:5002";
        private readonly HttpClient _httpClient = new();

        [Fact]
        public void FileMetadataService_LoadTest()
        {
            // Get user files scenario
            var getUserFilesScenario = Scenario.Create("get_user_files", async context =>
            {
                var userId = Guid.NewGuid();

                var request = Http.CreateRequest("GET", $"{_baseUrl}/api/files")
                    .WithHeader("userId", userId.ToString());

                var response = await Http.Send(_httpClient, request);
                return response;
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(10))
            .WithLoadSimulations(
                Simulation.Inject(20, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30)) // 20 RPS for 30 seconds
            );

            // Get file metadata scenario
            var getFileMetadataScenario = Scenario.Create("get_file_metadata", async context =>
            {
                var userId = Guid.NewGuid();
                var fileId = Guid.NewGuid();

                var request = Http.CreateRequest("GET", $"{_baseUrl}/api/files/{fileId}")
                    .WithHeader("userId", userId.ToString());

                var response = await Http.Send(_httpClient, request);
                return response;
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(10))
            .WithLoadSimulations(
                Simulation.Inject(30, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30)) // 30 RPS for 30 seconds
            );

            // Validate ownership scenario
            var validateOwnershipScenario = Scenario.Create("validate_ownership", async context =>
            {
                var userId = Guid.NewGuid();
                var fileId = Guid.NewGuid();

                var request = Http.CreateRequest("POST", $"{_baseUrl}/api/files/{fileId}/validate-ownership")
                    .WithHeader("userId", userId.ToString());

                var response = await Http.Send(_httpClient, request);
                return response;
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(10))
            .WithLoadSimulations(
                Simulation.Inject(50, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30)) // 50 RPS for 30 seconds
            );

            // Run all scenarios
            NBomberRunner
                .RegisterScenarios(getUserFilesScenario, getFileMetadataScenario, validateOwnershipScenario)
                .Run();
        }

        [Fact]
        public void FileMetadataService_StressTest()
        {
            var stressScenario = Scenario.Create("file_metadata_stress", async context =>
            {
                var operation = context.Random.Next(0, 3);
                var userId = Guid.NewGuid();

                return operation switch
                {
                    0 => await GetUserFilesOperation(userId),
                    1 => await GetFileMetadataOperation(userId),
                    2 => await ValidateOwnershipOperation(userId),
                    _ => await GetUserFilesOperation(userId)
                };
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(10))
            .WithLoadSimulations(
                Simulation.Inject(100, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60)) // 100 RPS for 1 minute
            );

            NBomberRunner
                .RegisterScenarios(stressScenario)
                .Run();
        }

        [Fact]
        public void FileMetadataService_ConcurrentUsersTest()
        {
            var concurrentScenario = Scenario.Create("file_metadata_concurrent", async context =>
            {
                var userId = Guid.NewGuid();

                var request = Http.CreateRequest("GET", $"{_baseUrl}/api/files")
                    .WithHeader("userId", userId.ToString());

                var response = await Http.Send(_httpClient, request);
                return response;
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(10))
            .WithLoadSimulations(
                Simulation.RampingInject(50, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30)), // Плавный рост до 50 RPS
                Simulation.Inject(100, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30)) // Постоянная нагрузка 100 RPS
            );

            NBomberRunner
                .RegisterScenarios(concurrentScenario)
                .Run();
        }

        private async Task<IResponse> GetUserFilesOperation(Guid userId)
        {
            var request = Http.CreateRequest("GET", $"{_baseUrl}/api/files")
                .WithHeader("userId", userId.ToString());

            return await Http.Send(_httpClient, request);
        }

        private async Task<IResponse> GetFileMetadataOperation(Guid userId)
        {
            var fileId = Guid.NewGuid();
            var request = Http.CreateRequest("GET", $"{_baseUrl}/api/files/{fileId}")
                .WithHeader("userId", userId.ToString());

            return await Http.Send(_httpClient, request);
        }

        private async Task<IResponse> ValidateOwnershipOperation(Guid userId)
        {
            var fileId = Guid.NewGuid();
            var request = Http.CreateRequest("POST", $"{_baseUrl}/api/files/{fileId}/validate-ownership")
                .WithHeader("userId", userId.ToString());

            return await Http.Send(_httpClient, request);
        }
    }
}
