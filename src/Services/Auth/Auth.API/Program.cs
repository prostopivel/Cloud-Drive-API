using Auth.API.HealthChecks;
using Auth.API.Middleware;
using Auth.Core.Interfaces.Repositories;
using Auth.Core.Interfaces.Services;
using Auth.Core.Services;
using Auth.Grpc.Services;
using Auth.Infrastructure.Data;
using Auth.Infrastructure.Repositories;
using Auth.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Common.Models;

namespace Auth.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen();

            // Configure DbContext
            builder.Services.AddDbContext<AuthDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Configure Redis
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration["Redis:ConnectionString"];
                options.InstanceName = builder.Configuration["Redis:InstanceName"];
            });

            // Configure settings
            builder.Services.Configure<JwtSettings>(
                builder.Configuration.GetSection("Jwt"));
            builder.Services.Configure<RedisSettings>(
                builder.Configuration.GetSection("Redis"));

            // Register dependencies
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<ICacheService, RedisCacheService>();
            builder.Services.AddScoped<IAuthService, AuthService>();

            // Add gRPC
            builder.Services.AddGrpc();

            builder.Services.AddHealthChecks()
                .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "auth-db")
                .AddRedis(builder.Configuration["Redis:ConnectionString"]!, name: "redis")
                .AddCheck<AuthServiceHealthCheck>("auth-service");

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.MapControllers();
            app.MapGrpcService<AuthGrpcService>();

            // Health check endpoint
            app.MapHealthChecks("/health");

            // Initialize database
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
                await context.Database.MigrateAsync();
            }

            app.Run();
        }
    }
}
