using Microsoft.Extensions.Caching.Distributed;

namespace Tests.Common.Extensions
{
    public static class DistributedCacheExtensions
    {
        public static async Task RemoveByPatternAsync(this IDistributedCache cache,
            string pattern)
        {
            var keysToRemove = new[]
            {
                $"{pattern.Replace("*", "")}"
            };

            foreach (var key in keysToRemove)
            {
                await cache.RemoveAsync(key);
            }
        }
    }
}
