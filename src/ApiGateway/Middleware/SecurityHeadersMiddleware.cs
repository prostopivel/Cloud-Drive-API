namespace ApiGateway.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers to all responses
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "DENY";
            context.Response.Headers.XXSSProtection = "1; mode=block";
            context.Response.Headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Remove server identification headers
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            await _next(context);
        }
    }
}
