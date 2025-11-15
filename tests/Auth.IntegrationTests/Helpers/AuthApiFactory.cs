using Auth.API;
using Auth.Core.Constants;
using Auth.Infrastructure.Data;
using IntegrationTests.Common;
using IntegrationTests.Common.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Auth.IntegrationTests.Helpers
{
    public class AuthApiFactory : BaseApiFactory<Program>
    {
        private readonly PostgreSqlContainer _postgresContainer;
        private readonly RedisContainer _redisContainer;

        public AuthApiFactory()
        {
            _postgresContainer = new PostgreSqlBuilder()
                .WithImage("postgres:15")
                .WithDatabase("auth_test")
                .WithUsername("test_user")
                .WithPassword("test_password")
                .Build();

            _redisContainer = new RedisBuilder()
                .WithImage("redis:7-alpine")
                .Build();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureDb<AuthDbContext>(_postgresContainer);
            builder.ConfigureCache(_redisContainer, "AuthTest");
        }

        public override async Task InitializeAsync()
        {
            await _postgresContainer.StartAsync();
            await _redisContainer.StartAsync();

            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            await context.Database.MigrateAsync();
        }

        public override async Task DisposeAsync()
        {
            await ResetAsync();

            await _postgresContainer.DisposeAsync();
            await _redisContainer.DisposeAsync();
        }

        public override async Task ResetAsync()
        {
            using var scope = Services.CreateScope();
            var services = scope.ServiceProvider;

            // Reset database
            var context = services.GetRequiredService<AuthDbContext>();

            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();

            // Reset cache
            var patterns = new[]
            {
                $"{CacheKeys.USER_BY_ID}:*",
                $"{CacheKeys.USER_BY_EMAIL}:*",
                $"{CacheKeys.TOKEN_USER}:*",
                $"{CacheKeys.TOKEN_VALIDATION}:*"
            };
            await _redisContainer.ClearCacheByPattern(patterns, "AuthTest:*");
        }
    }
}
