using System.Text;
using System.Text.Json;
using Bogus;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;

namespace Auth.LoadTests
{
    public class AuthLoadTests
    {
        private readonly string _baseUrl = "http://localhost:5001";
        private readonly Faker _faker = new Faker();
        private readonly HttpClient _httpClient = new HttpClient();

        [Fact]
        public void AuthService_LoadTest()
        {
            // Register scenario
            var registerScenario = Scenario.Create("auth_register", async context =>
            {
                var user = new
                {
                    username = _faker.Internet.UserName(),
                    email = _faker.Internet.Email(),
                    password = "Password123!"
                };

                var request = Http.CreateRequest("POST", $"{_baseUrl}/api/auth/register")
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json"));

                var response = await Http.Send(_httpClient, request);
                return response;
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(10))
            .WithLoadSimulations(
                Simulation.Inject(10, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30)) // 10 RPS for 30 seconds
            );

            // Login scenario
            var loginScenario = Scenario.Create("auth_login", async context =>
            {
                var user = new
                {
                    username = "testuser", // Pre-registered user
                    password = "Password123!"
                };

                var request = Http.CreateRequest("POST", $"{_baseUrl}/api/auth/login")
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json"));

                var response = await Http.Send(_httpClient, request);
                return response;
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(10))
            .WithLoadSimulations(
                Simulation.Inject(20, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30)) // 20 RPS for 30 seconds
            );

            // Token validation scenario
            var validateScenario = Scenario.Create("auth_validate", async context =>
            {
                var token = "valid.jwt.token.here"; // Should be a valid token
                var request = new
                {
                    token = token
                };

                var httpRequest = Http.CreateRequest("POST", $"{_baseUrl}/api/auth/validate")
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                var response = await Http.Send(_httpClient, httpRequest);
                return response;
            })
            .WithWarmUpDuration(TimeSpan.FromSeconds(10))
            .WithLoadSimulations(
                Simulation.Inject(50, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30)) // 50 RPS for 30 seconds
            );

            // Run all scenarios
            NBomberRunner
                .RegisterScenarios(registerScenario, loginScenario, validateScenario)
                .Run();
        }

        [Fact]
        public void AuthService_StressTest()
        {
            var stressScenario = Scenario.Create("auth_stress", async context =>
            {
                var operation = context.Random.Next(0, 3);

                return operation switch
                {
                    0 => await RegisterUser(),
                    1 => await LoginUser(),
                    2 => await ValidateToken(),
                    _ => await RegisterUser()
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

        private async Task<IResponse> RegisterUser()
        {
            var user = new
            {
                username = _faker.Internet.UserName() + _faker.Random.AlphaNumeric(10),
                email = _faker.Internet.Email(),
                password = "Password123!"
            };

            var request = Http.CreateRequest("POST", $"{_baseUrl}/api/auth/register")
                .WithHeader("Content-Type", "application/json")
                .WithBody(new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json"));

            return await Http.Send(_httpClient, request);
        }

        private async Task<IResponse> LoginUser()
        {
            var user = new
            {
                username = "testuser",
                password = "Password123!"
            };

            var request = Http.CreateRequest("POST", $"{_baseUrl}/api/auth/login")
                .WithHeader("Content-Type", "application/json")
                .WithBody(new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json"));

            return await Http.Send(_httpClient, request);
        }

        private async Task<IResponse> ValidateToken()
        {
            var request = new
            {
                token = "test.token.here"
            };

            var httpRequest = Http.CreateRequest("POST", $"{_baseUrl}/api/auth/validate")
                .WithHeader("Content-Type", "application/json")
                .WithBody(new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            return await Http.Send(_httpClient, httpRequest);
        }
    }
}
