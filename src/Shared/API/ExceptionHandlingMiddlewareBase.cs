using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Common.Exceptions;
using Shared.Common.Models;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;

namespace Shared.API
{
    public abstract class ExceptionHandlingMiddlewareBase
    {
        private readonly RequestDelegate _next;
        private readonly JsonSerializerOptions _jsonOptions;

        protected ILogger<ExceptionHandlingMiddlewareBase> Logger { get; init; }
        protected IHostEnvironment HostEnvironment { get; init; }

        public ExceptionHandlingMiddlewareBase(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddlewareBase> logger,
            IHostEnvironment environment)
        {
            _next = next;
            Logger = logger;
            HostEnvironment = environment;

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

        protected abstract Task HandleExceptionAsync(HttpContext context,
            Exception exception);

        protected async Task HandleCommonExceptionAsync(HttpContext context,
            Exception exception)
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
                        Details = HostEnvironment.IsDevelopment()
                            ? new { reason = exception.Message }
                            : null
                    };
                    Logger.LogInformation("Request was cancelled by the client");
                    break;

                case ValidationException validationEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response = new ErrorResponse
                    {
                        Error = validationEx.Message,
                        Code = "VALIDATION_ERROR",
                    };
                    Logger.LogWarning(validationEx, "Validation error occurred");
                    break;

                case ConflictException conflictEx:
                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    response = new ErrorResponse
                    {
                        Error = conflictEx.Message,
                        Code = "CONFLICT_ERROR",
                    };
                    Logger.LogWarning(conflictEx, "Conflict error occurred");
                    break;

                case NotFoundException notFoundEx:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response = new ErrorResponse
                    {
                        Error = notFoundEx.Message,
                        Code = "NOT_FOUND_ERROR",
                    };
                    Logger.LogWarning(notFoundEx, "NotFound error occurred");
                    break;

                case ForbidException forbidEx:
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    response = new ErrorResponse
                    {
                        Error = forbidEx.Message,
                        Code = "FORBID_ERROR",
                    };
                    Logger.LogWarning(forbidEx, "Forbid error occurred");
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response = new ErrorResponse
                    {
                        Error = HostEnvironment.IsDevelopment()
                            ? exception.Message
                            : "An internal server error occurred",
                        Code = "INTERNAL_ERROR"
                    };
                    Logger.LogError(exception, "Unhandled exception occurred");
                    break;
            }

            await WriteErrorAsync(context, exception, response);
        }

        protected async Task WriteErrorAsync(HttpContext context,
            Exception exception,
            ErrorResponse response)
        {
            if (HostEnvironment.IsDevelopment() && context.Response.StatusCode >= 500)
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
