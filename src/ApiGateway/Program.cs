using ApiGateway.Middleware;
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

            // Add gRPC clients for token validation
            builder.Services.AddGrpcClient<Auth.Grpc.AuthService.AuthServiceClient>(
                (serviceProvider, options) =>
                {
                    var servicesSettings = serviceProvider.GetRequiredService<IOptions<ServicesSettings>>().Value;
                    options.Address = new Uri(servicesSettings.Auth);
                });

            var app = builder.Build();

            app.MapReverseProxy();

            // Add middleware for token validation on protected routes
            app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/files"),
                appBuilder => appBuilder.UseMiddleware<TokenValidationMiddleware>());

            app.Run();
        }
    }
}
