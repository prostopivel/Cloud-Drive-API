using FileMetadata.API;
using FileMetadata.Infrastructure.Data;
using FileMetadata.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RabbitMQ.Client;
using Respawn;
using Shared.Common.Models;
using Shared.Messaging.Interfaces;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Tests.Common;
using Tests.Common.Extensions;
using Tests.Common.Mocks;

namespace FileMetadata.IntegrationTests.Helpers
{
    public class FileMetadataApiFactory : BaseApiFactory<Program>
    {
        private const string POSTGRE_CONTAINER_NAME = "postgres";
        private const string REDIS_CONTAINER_NAME = "redis";
        private const string RABBIT_MQ_CONTAINER_NAME = "rabbitmq";

        private readonly bool _useExistingContainers;
        private IMessageBus? _messageBus;
        private Respawner _respawner = null!;
        private string _connectionString = null!;
        private string _redisConnectionString = null!;
        private string _redisInstanceName = "FileMetadataTest";
        private string _rabbitMqConnectionString = null!;

        public IMessageBus MessageBus => _messageBus!;

        public FileMetadataApiFactory()
        {
            _useExistingContainers = CanConnectToExistingContainers();

            if (_useExistingContainers)
            {
                _connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? "Host=metadata-db;Database=metadata_db;Username=metadata_user;Password=metadata_password";

                _redisConnectionString = Environment.GetEnvironmentVariable("Redis__ConnectionString")
                    ?? "redis:6379";

                _rabbitMqConnectionString = Environment.GetEnvironmentVariable("RabbitMQ__ConnectionString")
                    ?? "amqp://guest:guest@rabbitmq:5672/";

                var instanceName = Environment.GetEnvironmentVariable("Redis__InstanceName");
                if (!string.IsNullOrEmpty(instanceName))
                {
                    _redisInstanceName = instanceName;
                }

                Console.WriteLine("Using existing containers from docker-compose");
                Console.WriteLine($"Database: {_connectionString}");
                Console.WriteLine($"Redis: {_redisConnectionString}");
                Console.WriteLine($"RabbitMQ: {_rabbitMqConnectionString}");
            }
            else
            {
                Containers.Add(POSTGRE_CONTAINER_NAME, new PostgreSqlBuilder()
                    .WithImage("postgres:15")
                    .WithDatabase("filemetadata_test")
                    .WithUsername("test_user")
                    .WithPassword("test_password")
                    .Build());

                Containers.Add(REDIS_CONTAINER_NAME, new RedisBuilder()
                    .WithImage("redis:7-alpine")
                    .Build());

                Containers.Add(RABBIT_MQ_CONTAINER_NAME, new RabbitMqBuilder()
                    .WithImage("rabbitmq:3-management")
                    .WithUsername("guest")
                    .WithPassword("guest")
                    .Build());

                _messageBus = new MockMessageBus();

                Console.WriteLine("Using Testcontainers");
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddEnvironmentVariables();
            });

            if (_useExistingContainers)
            {
                builder.ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;

                    // Override settings with environment variables
                    var dbSettings = configuration.GetSection("ConnectionStrings")
                        .Get<DatabaseSettings>();
                    var redisSettings = configuration.GetSection("Redis")
                        .Get<RedisSettings>();
                    var rabbitMqSettings = configuration.GetSection("RabbitMQ")
                        .Get<RabbitMQSettings>();

                    if (!string.IsNullOrEmpty(dbSettings?.ConnectionString))
                    {
                        _connectionString = dbSettings.ConnectionString;
                    }

                    if (!string.IsNullOrEmpty(redisSettings?.ConnectionString))
                    {
                        _redisConnectionString = redisSettings.ConnectionString;
                    }

                    if (!string.IsNullOrEmpty(redisSettings?.InstanceName))
                    {
                        _redisInstanceName = redisSettings.InstanceName;
                    }

                    if (!string.IsNullOrEmpty(rabbitMqSettings?.Host))
                    {
                        _rabbitMqConnectionString = $"amqp://{rabbitMqSettings.Username}:{rabbitMqSettings.Password}@{rabbitMqSettings.Host}:{rabbitMqSettings.Port}/";
                    }

                    var serviceProvider = services.BuildServiceProvider();
                    _messageBus = serviceProvider.GetService<IMessageBus>()!;

                    Console.WriteLine($"Final Database: {_connectionString}");
                    Console.WriteLine($"Final Redis: {_redisConnectionString}");
                    Console.WriteLine($"Final Redis Instance: {_redisInstanceName}");
                    Console.WriteLine($"Final RabbitMQ: {_rabbitMqConnectionString}");
                });
            }
            else
            {
                builder.ConfigureDb<FileMetadataDbContext>(
                    (PostgreSqlContainer)Containers[POSTGRE_CONTAINER_NAME]);
                builder.ConfigureCache(
                    (RedisContainer)Containers[REDIS_CONTAINER_NAME], _redisInstanceName);
                builder.ConfigureMessageBus<MockMessageConsumer>(
                    (RabbitMqContainer)Containers[RABBIT_MQ_CONTAINER_NAME],
                    _messageBus!);
            }

            builder.UseEnvironment("Testing");
        }

        public override async Task InitializeAsync()
        {
            if (_useExistingContainers)
            {
                await WaitForDatabase();
                await WaitForRedis();
                await WaitForRabbitMQ();
            }
            else
            {
                await Containers[POSTGRE_CONTAINER_NAME].StartAsync();
                await Containers[REDIS_CONTAINER_NAME].StartAsync();
                await Containers[RABBIT_MQ_CONTAINER_NAME].StartAsync();

                _connectionString = ((PostgreSqlContainer)Containers[POSTGRE_CONTAINER_NAME])
                    .GetConnectionString();
                _redisConnectionString = ((RedisContainer)Containers[REDIS_CONTAINER_NAME])
                    .GetConnectionString();
                _rabbitMqConnectionString = $"amqp://guest:guest@localhost:{((RabbitMqContainer)Containers[RABBIT_MQ_CONTAINER_NAME]).GetMappedPublicPort(5672)}/";
            }

            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FileMetadataDbContext>();

            await context.Database.MigrateAsync();

            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();

            _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"]
            });
        }

        public override async Task DisposeAsync()
        {
            await ResetAsync();

            if (!_useExistingContainers)
            {
                await Containers[POSTGRE_CONTAINER_NAME].DisposeAsync();
                await Containers[REDIS_CONTAINER_NAME].DisposeAsync();
                await Containers[RABBIT_MQ_CONTAINER_NAME].DisposeAsync();
            }
        }

        public async Task ResetAsync()
        {
            using var scope = Services.CreateScope();
            var services = scope.ServiceProvider;

            // Reset database using Respawn
            var context = services.GetRequiredService<FileMetadataDbContext>();
            var connection = context.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await _respawner.ResetAsync(connection);

            // Reset cache
            var cache = services.GetRequiredService<IDistributedCache>();
            List<string> patterns =
            [
                "file:*",
                "user:*",
                "metadata:*"
            ];

            foreach (var pattern in patterns)
            {
                await cache.RemoveByPatternAsync(pattern);
            }

            // Reset mock message bus
            (_messageBus as MockMessageBus)?.Clear();
        }

        protected override bool CanConnectToExistingContainers()
        {
            try
            {
                var postgresConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? "Host=metadata-db;Database=metadata_db;Username=metadata_user;Password=metadata_password";

                using var postgresConnection = new NpgsqlConnection(postgresConnectionString);
                postgresConnection.Open();
                postgresConnection.Close();

                var redisConnectionString = Environment.GetEnvironmentVariable("Redis__ConnectionString")
                    ?? "redis:6379";

                using var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
                var redisDb = redisConnection.GetDatabase();
                redisConnection.Close();

                var rabbitMqConnectionString = Environment.GetEnvironmentVariable("RabbitMQ__ConnectionString")
                    ?? "amqp://guest:guest@rabbitmq:5672/";

                var factory = new ConnectionFactory()
                {
                    Uri = new Uri(rabbitMqConnectionString)
                };
                using var rabbitMqConnection = factory.CreateConnection();
                rabbitMqConnection.Close();

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task WaitForDatabase()
        {
            const int maxAttempts = 30;
            var attempt = 0;

            Console.WriteLine($"Waiting for database: {_connectionString}");

            while (attempt < maxAttempts)
            {
                try
                {
                    using var connection = new NpgsqlConnection(_connectionString);
                    await connection.OpenAsync();

                    using var command = new NpgsqlCommand("SELECT 1;", connection);
                    await command.ExecuteScalarAsync();

                    await connection.CloseAsync();
                    Console.WriteLine("Database is available!");
                    return;
                }
                catch (Exception ex)
                {
                    attempt++;
                    Console.WriteLine($"Database attempt {attempt}/{maxAttempts} failed: {ex.Message}");

                    if (attempt >= maxAttempts)
                    {
                        throw new TimeoutException($"Database did not become available in time. Last error: {ex.Message}");
                    }
                    await Task.Delay(2000);
                }
            }
        }

        private async Task WaitForRedis()
        {
            const int maxAttempts = 30;
            var attempt = 0;

            Console.WriteLine($"Waiting for Redis: {_redisConnectionString}");

            while (attempt < maxAttempts)
            {
                try
                {
                    using var connection = await ConnectionMultiplexer.ConnectAsync(_redisConnectionString);
                    var db = connection.GetDatabase();
                    await db.PingAsync();

                    Console.WriteLine("Redis is available!");
                    return;
                }
                catch (Exception ex)
                {
                    attempt++;
                    Console.WriteLine($"Redis attempt {attempt}/{maxAttempts} failed: {ex.Message}");

                    if (attempt >= maxAttempts)
                    {
                        throw new TimeoutException($"Redis did not become available in time. Last error: {ex.Message}");
                    }
                    await Task.Delay(2000);
                }
            }
        }

        private async Task WaitForRabbitMQ()
        {
            const int maxAttempts = 30;
            var attempt = 0;

            Console.WriteLine($"Waiting for RabbitMQ: {_rabbitMqConnectionString}");

            while (attempt < maxAttempts)
            {
                try
                {
                    var factory = new ConnectionFactory()
                    {
                        Uri = new Uri(_rabbitMqConnectionString)
                    };
                    using var connection = factory.CreateConnection();
                    using var channel = connection.CreateModel();

                    Console.WriteLine("RabbitMQ is available!");
                    return;
                }
                catch (Exception ex)
                {
                    attempt++;
                    Console.WriteLine($"RabbitMQ attempt {attempt}/{maxAttempts} failed: {ex.Message}");

                    if (attempt >= maxAttempts)
                    {
                        throw new TimeoutException($"RabbitMQ did not become available in time. Last error: {ex.Message}");
                    }
                    await Task.Delay(2000);
                }
            }
        }
    }
}