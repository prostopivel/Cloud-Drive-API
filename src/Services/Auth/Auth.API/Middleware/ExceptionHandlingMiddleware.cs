using Auth.Core.Exceptions;
using Shared.Common.Exceptions;
using Shared.Common.Models;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;

namespace Auth.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;
        private readonly JsonSerializerOptions _jsonOptions;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            ErrorResponse response;

            switch (exception)
            {
                case OperationCanceledException or TaskCanceledException:
                    context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    response = new ErrorResponse
                    {
                        Error = "Request was cancelled",
                        Code = "REQUEST_CANCELLED",
                        Details = _env.IsDevelopment()
                            ? new { reason = exception.Message }
                            : null
                    };
                    _logger.LogInformation("Request was cancelled by the client");
                    break;

                case ValidationException validationEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response = new ErrorResponse
                    {
                        Error = validationEx.Message,
                        Code = "VALIDATION_ERROR",
                    };
                    _logger.LogWarning(validationEx, "Validation error occurred");
                    break;

                case ConflictException conflictnEx:
                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    response = new ErrorResponse
                    {
                        Error = conflictnEx.Message,
                        Code = "CONFLICT_ERROR",
                    };
                    _logger.LogWarning(conflictnEx, "Conflict error occurred");
                    break;

                case UnauthorizedException unauthorizedEx:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response = new ErrorResponse
                    {
                        Error = unauthorizedEx.Message,
                        Code = "UNAUTHORIZED_ERROR",
                    };
                    _logger.LogWarning(unauthorizedEx, "Unauthorized error occurred");
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response = new ErrorResponse
                    {
                        Error = _env.IsDevelopment()
                            ? exception.Message
                            : "An internal server error occurred",
                        Code = "INTERNAL_ERROR"
                    };
                    _logger.LogError(exception, "Unhandled exception occurred");
                    break;
            }

            if (_env.IsDevelopment() && context.Response.StatusCode >= 500)
            {
                response.Details = new
                {
                    stackTrace = exception.StackTrace,
                    exceptionType = exception.GetType().Name,
                    innerException = exception.InnerException?.Message
                };
            }

            var jsonResponse = JsonSerializer.Serialize(response, _jsonOptions);
            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
