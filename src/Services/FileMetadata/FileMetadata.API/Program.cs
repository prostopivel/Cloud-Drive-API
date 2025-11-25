using FileMetadata.API.HealthChecks;
using FileMetadata.API.Middleware;
using FileMetadata.Core.Interfaces.Repositories;
using FileMetadata.Core.Interfaces.Services;
using FileMetadata.Core.Services;
using FileMetadata.Infrastructure.Data;
using FileMetadata.Infrastructure.Repositories;
using FileMetadata.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Caching.Interfaces;
using Shared.Caching.Services;
using Shared.Common.Models;
using Shared.Messaging;
using Shared.Messaging.Interfaces;

namespace FileMetadata.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Configure settings
            builder.Services.Configure<DatabaseSettings>(
                builder.Configuration.GetSection("Database"));
            builder.Services.Configure<RabbitMQSettings>(
                builder.Configuration.GetSection("RabbitMQ"));
            builder.Services.Configure<RedisSettings>(
                builder.Configuration.GetSection("Redis"));

            // Database
            builder.Services.AddDbContext<FileMetadataDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Redis
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration["Redis:ConnectionString"];
                options.InstanceName = builder.Configuration["Redis:InstanceName"];
            });

            // Services
            builder.Services.AddScoped<IFileMetadataRepository, FileMetadataRepository>();
            builder.Services.AddScoped<ICacheService, RedisCacheService>();
            builder.Services.AddScoped<IFileMetadataService, FileMetadataService>();
            builder.Services.AddSingleton<IMessageBus, RabbitMQMessageBus>();
            builder.Services.AddSingleton<IMessageConsumer, RabbitMQMessageConsumer>();
            builder.Services.AddHostedService<MessageConsumerHostedService>();

            // Health checks
            builder.Services.AddHealthChecks()
                .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "metadata-db")
                .AddRedis(builder.Configuration["Redis:ConnectionString"]!, name: "redis")
                .AddCheck<FileMetadataHealthCheck>("file-metadata-service");

            var app = builder.Build();

            // Configure pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.MapControllers();
            app.MapHealthChecks("/health");

            // Initialize database
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<FileMetadataDbContext>();
                await context.Database.MigrateAsync();
            }

            app.Run();
        }
    }
}
