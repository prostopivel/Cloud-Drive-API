using Shared.Common.Exceptions;
using System.Net;

namespace Auth.Core.Exceptions
{
    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message)
            : base(message, HttpStatusCode.Unauthorized)
        { }
    }
}
