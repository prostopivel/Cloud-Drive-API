using StackExchange.Redis;
using Testcontainers.Redis;

namespace IntegrationTests.Common.Extensions
{
    public static class RedisContainerExtensions
    {
        public static async Task ClearCacheByPattern(this RedisContainer redisContainer,
            string[] patterns,
            string keysPattern)
        {
            ConnectionMultiplexer? redisConnection = null;
            try
            {
                var redisConnectionString = redisContainer.GetConnectionString();
                redisConnection = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
                var database = redisConnection.GetDatabase();
                var endpoints = redisConnection.GetEndPoints();
                var server = redisConnection.GetServer(endpoints.First());

                var keysToDelete = new List<RedisKey>();

                foreach (var pattern in patterns)
                {
                    var keys = server.Keys(pattern: pattern).ToArray();
                    keysToDelete.AddRange(keys);
                }

                var instanceKeys = server.Keys(pattern: keysPattern).ToArray();
                keysToDelete.AddRange(instanceKeys);

                if (keysToDelete.Count != 0)
                {
                    await database.KeyDeleteAsync([.. keysToDelete.Distinct()]);
                    Console.WriteLine($"Cleared {keysToDelete.Count} cache keys");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing cache by pattern: {ex.Message}");
            }
            finally
            {
                redisConnection?.Close();
                redisConnection?.Dispose();
            }
        }
    }
}
