using ApiGateway.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shared.Common.Models;
using System.Text.Json;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("swagger-aggregate")]
    public class SwaggerController : ControllerBase
    {
        private readonly ISwaggerAggregatorService _swaggerAggregator;
        private readonly ServicesSettings _servicesSettings;
        private readonly ILogger<SwaggerController> _logger;

        public SwaggerController(
            ISwaggerAggregatorService swaggerAggregator,
            IOptions<ServicesSettings> servicesSettings,
            ILogger<SwaggerController> logger)
        {
            _swaggerAggregator = swaggerAggregator;
            _servicesSettings = servicesSettings.Value;
            _logger = logger;
        }

        [HttpGet("all.json")]
        public async Task<IActionResult> GetAggregatedSwagger()
        {
            var services = new[]
            {
                    new { Name = "Auth Service", Url = _servicesSettings.Auth },
                    new { Name = "File Metadata Service", Url = _servicesSettings.FileMetadata },
                    new { Name = "File Storage Service", Url = _servicesSettings.FileStorage }
                };

            var aggregatedDoc = new
            {
                openapi = "3.0.1",
                info = new
                {
                    title = "Cloud Drive API - All Services",
                    version = "1.0",
                    description = "Aggregated API documentation for all microservices"
                },
                paths = new Dictionary<string, object>(),
                components = new
                {
                    schemas = new Dictionary<string, object>(),
                    securitySchemes = new Dictionary<string, object>()
                }
            };

            foreach (var service in services)
            {
                if (string.IsNullOrEmpty(service.Url))
                {
                    continue;
                }

                var swaggerDoc = await _swaggerAggregator.GetSwaggerJsonAsync(service.Url);
                if (swaggerDoc != null)
                {
                    MergeSwaggerDocuments(aggregatedDoc, swaggerDoc, service.Name);
                }
            }

            return Ok(aggregatedDoc);
        }

        private static void MergeSwaggerDocuments(dynamic aggregatedDoc,
            JsonDocument serviceDoc,
            string serviceName)
        {
            if (serviceDoc.RootElement.TryGetProperty("paths", out var paths))
            {
                foreach (var path in paths.EnumerateObject())
                {
                    var prefixedPath = $"/{serviceName.ToLower().Replace(" ", "-")}{path.Name}";
                    aggregatedDoc.paths[prefixedPath] = path.Value;
                }
            }

            if (serviceDoc.RootElement.TryGetProperty("components", out var components)
                && components.TryGetProperty("schemas", out var schemas))
            {
                foreach (var schema in schemas.EnumerateObject())
                {
                    aggregatedDoc.components.schemas[schema.Name] = schema.Value;
                }
            }
        }
    }
}
