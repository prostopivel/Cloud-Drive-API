using FileStorage.API.HealthChecks;
using FileStorage.API.Middleware;
using FileStorage.Core.Interfaces.Repositories;
using FileStorage.Core.Interfaces.Services;
using FileStorage.Core.Services;
using FileStorage.Infrastructure.Repositories;
using Microsoft.Extensions.Options;
using Shared.Common.Models;
using Shared.Messaging;
using Shared.Messaging.Interfaces;

namespace FileStorage.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Configure settings
            builder.Services.Configure<FileStorageSettings>(
                builder.Configuration.GetSection("FileStorage"));

            // Register services
            builder.Services.AddScoped<IFileStorageRepository, LocalFileStorageRepository>();
            builder.Services.AddSingleton<IMessageBus, RabbitMQMessageBus>();
            builder.Services.AddScoped<IFileStorageService, FileStorageService>();

            // Health checks
            builder.Services.AddHealthChecks()
                .AddCheck<FileStorageHealthCheck>("file-storage");

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
            app.MapHealthChecks("/health");

            // Ensure storage directory exists
            var storageSettings = app.Services.GetRequiredService<IOptions<FileStorageSettings>>();
            Directory.CreateDirectory(storageSettings.Value.StoragePath);

            app.Run();
        }
    }
}
