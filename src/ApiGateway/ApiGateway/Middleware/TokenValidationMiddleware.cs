using Grpc.Net.Client;

namespace ApiGateway.Middleware
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public TokenValidationMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip token validation for public routes
            if (context.Request.Path.StartsWithSegments("/api/auth"))
            {
                await _next(context);
                return;
            }

            // Extract token from Authorization header
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Token is required");
                return;
            }

            // Validate token via gRPC call to Auth Service
            using var channel = GrpcChannel.ForAddress(_configuration["Services:Auth"]!);
            //var client = new Auth.Grpc.AuthService.AuthServiceClient(channel);

            try
            {
                //var response = await client.ValidateTokenAsync(new Auth.Grpc.TokenRequest { Token = token });

                //if (!response.IsValid)
                //{
                //    context.Response.StatusCode = 401;
                //    await context.Response.WriteAsync("Invalid token");
                //    return;
                //}

                //// Add user info to context for downstream services
                //context.Items["UserId"] = response.UserId;
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync($"Token validation error: {ex.Message}");
                return;
            }

            await _next(context);
        }
    }
}
