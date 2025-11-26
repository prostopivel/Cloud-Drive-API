using ApiGateway.Extensions;
using ApiGateway.Interfaces;
using ApiGateway.Middleware;
using ApiGateway.Services;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Shared.Common.Models;

namespace ApiGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<ServicesSettings>(
                builder.Configuration.GetSection("Services"));

            // Add YARP
            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            // Add services
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.ConfigureSwagger();

            builder.Services.AddScoped<IUserIdValidator, UserIdValidator>();
            builder.Services.AddScoped<IFilesService, FilesService>();

            // Add HTTP client for service communication
            builder.Services.AddHttpClient("AuthService",
                (serviceProvider, client) =>
                {
                    var servicesSettings = serviceProvider.GetRequiredService<IOptions<ServicesSettings>>().Value;
                    client.BaseAddress = new Uri(servicesSettings.Auth);
                });

            builder.Services.AddHttpClient("FileMetadataService",
                (serviceProvider, client) =>
                {
                    var servicesSettings = serviceProvider.GetRequiredService<IOptions<ServicesSettings>>().Value;
                    client.BaseAddress = new Uri(servicesSettings.FileMetadata);
                });

            builder.Services.AddHttpClient("FileStorageService",
                (serviceProvider, client) =>
                {
                    var servicesSettings = serviceProvider.GetRequiredService<IOptions<ServicesSettings>>().Value;
                    client.BaseAddress = new Uri(servicesSettings.FileStorage);
                });

            // Add gRPC client for token validation
            builder.Services.AddGrpcClient<Auth.Grpc.AuthService.AuthServiceClient>((serviceProvider, options) =>
            {
                var servicesSettings = serviceProvider.GetRequiredService<IOptions<ServicesSettings>>().Value;
                options.Address = new Uri(servicesSettings.AuthGrpc ?? servicesSettings.Auth);
            });

            // Add health checks
            builder.Services.AddHealthChecks()
                .AddUrlGroup(serviceProvider =>
                {
                    var servicesSettings = serviceProvider.GetRequiredService<IOptions<ServicesSettings>>().Value;
                    return new Uri(servicesSettings.Auth + "/health");
                }, "auth-service")
                .AddUrlGroup(serviceProvider =>
                {
                    var servicesSettings = serviceProvider.GetRequiredService<IOptions<ServicesSettings>>().Value;
                    return new Uri(servicesSettings.FileMetadata + "/health");
                }, "filemetadata-service")
                .AddUrlGroup(serviceProvider =>
                {
                    var servicesSettings = serviceProvider.GetRequiredService<IOptions<ServicesSettings>>().Value;
                    return new Uri(servicesSettings.FileStorage + "/health");
                }, "filestorage-service");

            var app = builder.Build();

            // Configure pipeline
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cloud Drive API Gateway v1");
                c.RoutePrefix = "swagger";
                c.DisplayRequestDuration();
                c.EnableDeepLinking();
                c.EnableFilter();
            });

            app.UseRouting();

            app.UseMiddleware<SecurityHeadersMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            // Add middleware for token validation on protected routes
            app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/files"),
                appBuilder => appBuilder.UseMiddleware<TokenValidationMiddleware>());

            app.MapReverseProxy();
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
            app.MapControllers();

            app.Run();
        }
    }
}