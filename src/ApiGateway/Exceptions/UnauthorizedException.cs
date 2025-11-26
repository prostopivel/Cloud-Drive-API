using Shared.Common.Exceptions;
using System.Net;

namespace ApiGateway.Exceptions
{
    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message)
            : base(message, HttpStatusCode.Unauthorized)
        { }
    }
}
