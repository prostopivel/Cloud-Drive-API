using ApiGateway.Middleware;

namespace ApiGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add YARP
            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            // Add gRPC clients for token validation
            //builder.Services.AddGrpcClient<Auth.Grpc.AuthService.AuthServiceClient>(options =>
            //{
            //    options.Address = new Uri(builder.Configuration["Services:Auth"]!);
            //});

            var app = builder.Build();

            app.MapReverseProxy();

            // Add middleware for token validation on protected routes
            app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/files"),
                appBuilder => appBuilder.UseMiddleware<TokenValidationMiddleware>());

            app.Run();
        }
    }
}
