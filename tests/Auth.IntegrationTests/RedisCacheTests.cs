using Auth.API;
using Auth.Core.Interfaces.Services;
using Auth.IntegrationTests.DTOs;
using Auth.IntegrationTests.Helpers;
using FluentAssertions;
using Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Auth.IntegrationTests
{
    public class RedisCacheTests : BaseTests<AuthApiFactory, Program>
    {
        public RedisCacheTests(AuthApiFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task CacheService_CanStoreAndRetrieveData()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
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
            using var scope = _factory.Services.CreateScope();
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
            using var scope = _factory.Services.CreateScope();
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
            using var scope = _factory.Services.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var iterations = 100;
            var stopwatch = new System.Diagnostics.Stopwatch();

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
    }
}
