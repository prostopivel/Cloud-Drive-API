using ApiGateway.Exceptions;
using Shared.API;
using Shared.Common.Models;
using System.Net;

namespace ApiGateway.Middleware
{
    public class ExceptionHandlingMiddleware : ExceptionHandlingMiddlewareBase
    {
        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment env)
            : base(next, logger, env) { }

        protected override async Task HandleExceptionAsync(HttpContext context,
            Exception exception)
        {
            context.Response.ContentType = "application/json";

            ErrorResponse response;

            switch (exception)
            {
                case UnauthorizedException unauthorizedEx:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response = new ErrorResponse
                    {
                        Error = unauthorizedEx.Message,
                        Code = "UNAUTHORIZED_ERROR",
                    };
                    Logger.LogWarning(unauthorizedEx, "Unauthorized error occurred");
                    break;

                default:
                    await HandleCommonExceptionAsync(context, exception);
                    return;
            }

            await WriteErrorAsync(context, exception, response);
        }
    }
}
