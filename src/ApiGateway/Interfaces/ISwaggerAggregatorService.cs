using System.Text.Json;

namespace ApiGateway.Interfaces
{
    public interface ISwaggerAggregatorService
    {
        Task<JsonDocument?> GetSwaggerJsonAsync(string serviceUrl);
    }

}
