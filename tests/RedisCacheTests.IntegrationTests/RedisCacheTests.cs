using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RedisCacheTests.IntegrationTests.DTOs;
using Shared.Caching.Interfaces;
using Shared.Caching.Services;
using StackExchange.Redis;
using System.Diagnostics;
using Xunit;

namespace RedisCacheTests.IntegrationTests
{
    public class RedisCacheTests : IAsyncLifetime
    {
        private IContainer _redisContainer = null!;
        private ConnectionMultiplexer _redis = null!;
        private IDatabase _db = null!;
        private ServiceProvider _serviceProvider = null!;

        public async Task InitializeAsync()
        {
            // Запускаем Redis контейнер
            _redisContainer = new ContainerBuilder()
                .WithImage("redis:7-alpine")
                .WithPortBinding(6379, true)
                .Build();

            await _redisContainer.StartAsync();

            var port = _redisContainer.GetMappedPublicPort(6379);
            var connectionString = $"localhost:{port}";

            await WaitForRedisReady(connectionString);

            _redis = ConnectionMultiplexer.Connect(connectionString);
            _db = _redis.GetDatabase();

            var services = new ServiceCollection();

            services.AddSingleton<IConnectionMultiplexer>(_redis);

            services.AddDistributedMemoryCache();
            services.AddLogging();
            services.AddScoped<ICacheService, RedisCacheService>();

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task CacheService_CanStoreAndRetrieveData()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var testData = new TestData { Id = 1, Name = "Test", Value = 123.45m };

            // Act
            await cacheService.SetAsync("test-key", testData, TimeSpan.FromMinutes(1));
            var retrievedData = await cacheService.GetAsync<TestData>("test-key");

            // Assert
            retrievedData.Should().NotBeNull();
            retrievedData!.Id.Should().Be(testData.Id);
            retrievedData.Name.Should().Be(testData.Name);
            retrievedData.Value.Should().Be(testData.Value);
        }

        [Fact]
        public async Task CacheService_ReturnsNullForNonExistentKey()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            // Act
            var result = await cacheService.GetAsync<string>("non-existent-key");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CacheService_CanRemoveData()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            await cacheService.SetAsync("remove-test", "test-value", TimeSpan.FromMinutes(1));

            // Act
            await cacheService.RemoveAsync("remove-test");
            var result = await cacheService.GetAsync<string>("remove-test");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CacheService_PerformanceTest()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var iterations = 100;
            var stopwatch = new Stopwatch();

            // Act - Write performance
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                await cacheService.SetAsync($"perf-key-{i}", new TestData { Id = i, Name = $"Test{i}" });
            }
            stopwatch.Stop();
            var writeTime = stopwatch.ElapsedMilliseconds;

            // Read performance
            stopwatch.Restart();
            for (int i = 0; i < iterations; i++)
            {
                await cacheService.GetAsync<TestData>($"perf-key-{i}");
            }
            stopwatch.Stop();
            var readTime = stopwatch.ElapsedMilliseconds;

            // Assert - Should complete within reasonable time
            writeTime.Should().BeLessThan(1000); // 1 second for 100 writes
            readTime.Should().BeLessThan(500);   // 0.5 seconds for 100 reads
        }

        [Fact]
        public async Task BasicRedisOperations_ShouldWork()
        {
            // Arrange
            var key = "direct-test";
            var value = "direct-value";

            // Act
            await _db.StringSetAsync(key, value);
            var result = await _db.StringGetAsync(key);

            // Assert
            result.HasValue.Should().BeTrue();
            result.ToString().Should().Be(value);
        }

        private async Task WaitForRedisReady(string connectionString)
        {
            var maxAttempts = 30;
            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    using var testRedis = ConnectionMultiplexer.Connect(connectionString);
                    var testDb = testRedis.GetDatabase();
                    await testDb.PingAsync();
                    testRedis.Dispose();
                    return;
                }
                catch
                {
                    await Task.Delay(1000);
                }
            }
            throw new TimeoutException("Redis container failed to start in time");
        }

        public async Task DisposeAsync()
        {
            _serviceProvider?.Dispose();
            _redis?.Dispose();

            if (_redisContainer != null)
            {
                await _redisContainer.StopAsync();
                await _redisContainer.DisposeAsync();
            }
        }
    }
}