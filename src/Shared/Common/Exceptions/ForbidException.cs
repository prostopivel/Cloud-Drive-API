using System.Net;

namespace Shared.Common.Exceptions
{
    public class ForbidException : AppException
    {
        public ForbidException(string message)
            : base(message, HttpStatusCode.Forbidden)
        { }
    }
}
