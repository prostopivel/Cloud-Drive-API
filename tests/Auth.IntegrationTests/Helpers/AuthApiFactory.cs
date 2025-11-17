using Auth.API;
using Auth.Core.Constants;
using Auth.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Shared.Common.Models;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Tests.Common;
using Tests.Common.Extensions;

namespace Auth.IntegrationTests.Helpers
{
    public class AuthApiFactory : BaseApiFactory<Program>
    {
        private const string POSTGRE_CONTAINER_NAME = "postres";
        private const string REDIS_CONTAINER_NAME = "redis";

        private readonly bool _useExistingContainers;
        private Respawner _respawner = null!;
        private string _connectionString = null!;
        private string _redisConnectionString = null!;
        private string _redisInstanceName = null!;

        public AuthApiFactory()
            : base()
        {
            _useExistingContainers = CanConnectToExistingContainers();

            if (_useExistingContainers)
            {
                _connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? "Host=auth-db;Database=auth_db;Username=auth_user;Password=auth_password";

                _redisConnectionString = Environment.GetEnvironmentVariable("Redis__ConnectionString")
                    ?? "redis:6379";
                _redisInstanceName = Environment.GetEnvironmentVariable("Redis__InstanceName")
                    ?? "AuthIntegrationTests";

                Console.WriteLine("Using existing containers from docker-compose");
                Console.WriteLine($"Database: {_connectionString}");
                Console.WriteLine($"Redis: {_redisConnectionString}");
            }
            else
            {
                Containers.Add(POSTGRE_CONTAINER_NAME, new PostgreSqlBuilder()
                    .WithImage("postgres:15")
                    .WithDatabase("auth_test")
                    .WithUsername("test_user")
                    .WithPassword("test_password")
                    .Build());

                Containers.Add(REDIS_CONTAINER_NAME, new RedisBuilder()
                    .WithImage("redis:7-alpine")
                    .Build());

                _redisInstanceName = "AuthTest";
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

                    var dbSettings = configuration.GetSection("ConnectionStrings")
                        .Get<DatabaseSettings>();
                    var redisSettings = configuration.GetSection("Redis")
                        .Get<RedisSettings>();

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

                    services.Configure<DatabaseSettings>(
                        configuration.GetSection("ConnectionStrings"));
                    services.Configure<RedisSettings>(
                        configuration.GetSection("Redis"));

                    Console.WriteLine($"Final Database: {_connectionString}");
                    Console.WriteLine($"Final Redis: {_redisConnectionString}");
                    Console.WriteLine($"Final Redis Instance: {_redisInstanceName}");
                });
            }
            else
            {
                builder.ConfigureDb<AuthDbContext>((PostgreSqlContainer)Containers[POSTGRE_CONTAINER_NAME]);
                builder.ConfigureCache((RedisContainer)Containers[REDIS_CONTAINER_NAME], _redisInstanceName);
            }

            builder.UseEnvironment("Testing");
        }

        public override async Task InitializeAsync()
        {
            if (_useExistingContainers)
            {
                await WaitForDatabase();
                await WaitForRedis();
            }
            else
            {
                await Containers[POSTGRE_CONTAINER_NAME].StartAsync();
                await Containers[REDIS_CONTAINER_NAME].StartAsync();

                _connectionString = ((PostgreSqlContainer)Containers[POSTGRE_CONTAINER_NAME])
                    .GetConnectionString();
                _redisConnectionString = ((RedisContainer)Containers[REDIS_CONTAINER_NAME])
                    .GetConnectionString();
            }

            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

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
            }
        }

        public override async Task ResetAsync()
        {
            using var scope = Services.CreateScope();
            var services = scope.ServiceProvider;

            // Reset database using Respawn
            var context = services.GetRequiredService<AuthDbContext>();
            var connection = context.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            await _respawner.ResetAsync(connection);

            // Reset cache
            var cache = services.GetRequiredService<IDistributedCache>();
            List<string> patterns =
            [
                $"{CacheKeys.USER_BY_ID}:*",
                $"{CacheKeys.USER_BY_EMAIL}:*",
                $"{CacheKeys.TOKEN_USER}:*",
                $"{CacheKeys.TOKEN_VALIDATION}:*"
            ];

            foreach (var pattern in patterns)
            {
                await cache.RemoveByPatternAsync(pattern);
            }
        }

        protected override bool CanConnectToExistingContainers()
        {
            try
            {
                var postgresConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? "Host=auth-db;Database=auth_db;Username=auth_user;Password=auth_password";

                using var postgresConnection = new NpgsqlConnection(postgresConnectionString);
                postgresConnection.Open();
                postgresConnection.Close();

                var redisConnectionString = Environment.GetEnvironmentVariable("Redis__ConnectionString")
                    ?? "redis:6379";

                using var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
                var redisDb = redisConnection.GetDatabase();
                redisConnection.Close();

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
    }
}