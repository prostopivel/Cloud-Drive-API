using Auth.Core.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Auth.Infrastructure.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly DistributedCacheEntryOptions _defaultOptions;

        public RedisCacheService(IDistributedCache cache,
            ILogger<RedisCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
            _defaultOptions = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(30)
            };
        }

        public async Task<T?> GetAsync<T>(string key,
            CancellationToken token = default)
        {
            try
            {
                var cachedData = await _cache.GetStringAsync(key, token: token);
                if (string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogDebug("Cache miss for key: {Key}", key);
                    return default;
                }

                _logger.LogDebug("Cache hit for key: {Key}", key);
                return JsonSerializer.Deserialize<T>(cachedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache for key: {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key,
            T value,
            TimeSpan? expiry = null,
            CancellationToken token = default)
        {
            try
            {
                var options = expiry.HasValue
                    ? new DistributedCacheEntryOptions { SlidingExpiration = expiry }
                    : _defaultOptions;

                var serializedData = JsonSerializer.Serialize(value);
                await _cache.SetStringAsync(key, serializedData, options, token: token);

                _logger.LogDebug("Cache set for key: {Key} with expiry: {Expiry}", key, options.SlidingExpiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key,
            CancellationToken token = default)
        {
            try
            {
                await _cache.RemoveAsync(key, token: token);
                _logger.LogDebug("Cache removed for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache for key: {Key}", key);
            }
        }

        public async Task<bool> ExistsAsync(string key,
            CancellationToken token = default)
        {
            try
            {
                var cachedData = await _cache.GetStringAsync(key, token: token);
                return !string.IsNullOrEmpty(cachedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
                return false;
            }
        }
    }
}
