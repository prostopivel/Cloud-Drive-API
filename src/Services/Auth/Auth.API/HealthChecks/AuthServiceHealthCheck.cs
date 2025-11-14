using Auth.Core.Interfaces.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Auth.API.HealthChecks
{
    public class AuthServiceHealthCheck : IHealthCheck
    {
        private readonly IAuthService _authService;
        private readonly ICacheService _cacheService;

        public AuthServiceHealthCheck(IAuthService authService, ICacheService cacheService)
        {
            _authService = authService;
            _cacheService = cacheService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken token = default)
        {
            try
            {
                // Test cache service
                var testKey = "health_check_test";
                await _cacheService.SetAsync(testKey, "test_value",
                    TimeSpan.FromSeconds(30), token: token);
                var cachedValue = await _cacheService.GetAsync<string>(testKey, token: token);

                if (cachedValue != "test_value")
                {
                    return HealthCheckResult.Degraded("Redis cache is not working properly");
                }

                // Test token service (basic functionality)
                var testToken = "test";
                var isValid = await _authService.ValidateTokenAsync(testToken, token: token);

                // We expect false for invalid token, but the method should not throw

                return HealthCheckResult.Healthy("Auth service is healthy");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Auth service is unhealthy", ex);
            }
        }
    }
}
