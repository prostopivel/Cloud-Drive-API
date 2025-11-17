using Shared.Common.Exceptions;
using System.Net;

namespace FileStorage.Core.Exceptions
{
    public class InvalidFileException : AppException
    {
        public InvalidFileException(string message)
            : base(message, HttpStatusCode.BadRequest)
        { }
    }
}
