using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Tests.Common.Extensions
{
    public static class IWebHostBuilderExtensions
    {
        public static void ConfigureDb<K>(this IWebHostBuilder builder,
            PostgreSqlContainer postgresContainer)
            where K : DbContext
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove existing DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<K>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add DbContext with test database
                services.AddDbContext<K>(options =>
                    options.UseNpgsql(postgresContainer.GetConnectionString()));
            });
        }

        public static void ConfigureCache(this IWebHostBuilder builder,
            RedisContainer redisContainer,
            string cacheName)
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove existing Redis cache
                var redisDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IDistributedCache));
                if (redisDescriptor != null)
                {
                    services.Remove(redisDescriptor);
                }

                // Add Redis cache with test container
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisContainer.GetConnectionString();
                    options.InstanceName = cacheName;
                });
            });
        }
    }
}
