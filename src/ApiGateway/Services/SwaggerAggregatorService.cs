using ApiGateway.Interfaces;
using System.Text.Json;

namespace ApiGateway.Services
{
    public class SwaggerAggregatorService : ISwaggerAggregatorService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SwaggerAggregatorService> _logger;

        public SwaggerAggregatorService(HttpClient httpClient,
            ILogger<SwaggerAggregatorService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<JsonDocument?> GetSwaggerJsonAsync(string serviceUrl)
        {
            var swaggerUrl = $"{serviceUrl}/swagger/v1/swagger.json";
            _logger.LogInformation("Fetching Swagger from: {SwaggerUrl}", swaggerUrl);

            var response = await _httpClient.GetAsync(swaggerUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(content);
            }

            _logger.LogWarning("Failed to fetch Swagger from {ServiceUrl}. Status: {StatusCode}",
                serviceUrl, response.StatusCode);
            return null;
        }
    }
}
