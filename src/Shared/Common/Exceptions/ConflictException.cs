using System.Net;

namespace Shared.Common.Exceptions
{
    public class ConflictException : AppException
    {
        public ConflictException(string message)
            : base(message, HttpStatusCode.Conflict)
        { }
    }
}
