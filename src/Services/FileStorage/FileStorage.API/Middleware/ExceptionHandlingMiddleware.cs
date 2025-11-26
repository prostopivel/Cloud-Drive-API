using FileStorage.Core.Exceptions;
using Shared.API;
using Shared.Common.Models;
using System.Net;

namespace FileStorage.API.Middleware
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
                case InvalidFileException invalidFileEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response = new ErrorResponse
                    {
                        Error = invalidFileEx.Message,
                        Code = "INVALID_FILE_ERROR",
                    };
                    Logger.LogWarning(invalidFileEx, "InvalidFile error occurred");
                    break;

                default:
                    await HandleCommonExceptionAsync(context, exception);
                    return;
            }

            await WriteErrorAsync(context, exception, response);
        }
    }
}
