using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using Shared.Common.Models;

namespace ApiGateway.Middleware
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ServicesSettings _servicesSettings;
        private readonly ILogger<TokenValidationMiddleware> _logger;

        public TokenValidationMiddleware(RequestDelegate next,
            IOptions<ServicesSettings> servicesSettings,
            ILogger<TokenValidationMiddleware> logger)
        {
            _next = next;
            _servicesSettings = servicesSettings.Value;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip token validation for auth routes and storage routes
            if (context.Request.Path.StartsWithSegments("/api/auth") ||
                context.Request.Path.StartsWithSegments("/storage"))
            {
                await _next(context);
                return;
            }

            // Extract token from Authorization header
            var token = context.Request.Headers.Authorization
                .FirstOrDefault()?
                .Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Token is required");
                return;
            }

            try
            {
                // Create gRPC channel and client
                using var channel = GrpcChannel.ForAddress(_servicesSettings.Auth);
                var client = new Auth.Grpc.AuthService.AuthServiceClient(channel);

                // Validate token via gRPC
                var response = await client.ValidateTokenAsync(
                    new Auth.Grpc.TokenRequest { Token = token });

                if (!response.IsValid)
                {
                    _logger.LogWarning("Invalid token received");
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Invalid token");
                    return;
                }

                // Add user info to context for downstream services
                context.Items["UserId"] = response.UserId;
                context.Items["Username"] = response.Username;

                _logger.LogInformation("Token validated for user {UserId}", response.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token via gRPC");
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync($"Token validation error: {ex.Message}");
                return;
            }

            await _next(context);
        }
    }
}
