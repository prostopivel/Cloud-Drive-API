using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Common.Models;
using Shared.Messaging.Interfaces;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
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
                // Remove existing DbContext settings
                var dbSettingsDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IConfigureOptions<DatabaseSettings>));
                if (dbSettingsDescriptor != null)
                {
                    services.Remove(dbSettingsDescriptor);
                }

                // Add DbContext with test container settings
                services.Configure<DatabaseSettings>(options =>
                {
                    options.ConnectionString = postgresContainer.GetConnectionString();
                });

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
                // Remove existing Redis settings
                var redisSettingsDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IConfigureOptions<RedisSettings>));
                if (redisSettingsDescriptor != null)
                {
                    services.Remove(redisSettingsDescriptor);
                }

                // Add Redis with test container settings
                services.Configure<RedisSettings>(options =>
                {
                    options.ConnectionString = redisContainer.GetConnectionString();
                });

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

        public static void ConfigureMessageBus<T>(this IWebHostBuilder builder,
            RabbitMqContainer rabbitMqContainer,
            IMessageBus messageBus)
            where T : IMessageConsumer
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove existing RabbitMQ settings
                var rabbitMqSettingsDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IConfigureOptions<RabbitMQSettings>));
                if (rabbitMqSettingsDescriptor != null)
                {
                    services.Remove(rabbitMqSettingsDescriptor);
                }

                // Add RabbitMQ with test container settings
                services.Configure<RabbitMQSettings>(options =>
                {
                    options.Host = rabbitMqContainer.Hostname;
                    options.Port = rabbitMqContainer.GetMappedPublicPort(5672);
                    options.Username = "guest";
                    options.Password = "guest";
                });

                // Remove existing MessageConsumer service
                var hostedServiceDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IMessageConsumer));
                if (hostedServiceDescriptor != null)
                {
                    services.Remove(hostedServiceDescriptor);
                }

                // Add MessageConsumer service
                services.AddSingleton(typeof(IMessageConsumer), typeof(T));

                // Remove existing RabbitMQ hosted service
                var rabbitMqDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IConnectionFactory));
                if (rabbitMqDescriptor != null)
                {
                    services.Remove(rabbitMqDescriptor);
                }

                // Remove existing IConnection if registered
                var connectionDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IConnection));
                if (connectionDescriptor != null)
                {
                    services.Remove(connectionDescriptor);
                }

                // Remove existing IModel if registered
                var channelDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IModel));
                if (channelDescriptor != null)
                {
                    services.Remove(channelDescriptor);
                }

                // Add RabbitMQ with test container
                services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
                {
                    Uri = new Uri(rabbitMqContainer.GetConnectionString()),
                    DispatchConsumersAsync = true
                });

                // Remove IMessageBus with mock
                var messageBusDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IMessageBus));
                if (messageBusDescriptor != null)
                    services.Remove(messageBusDescriptor);

                // Add IMessageBus with with mock
                services.AddSingleton(messageBus);
            });
        }
    }
}
