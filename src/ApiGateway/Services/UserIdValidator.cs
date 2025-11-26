using ApiGateway.Exceptions;
using ApiGateway.Interfaces;

namespace ApiGateway.Services
{
    public class UserIdValidator : IUserIdValidator
    {
        public Guid Validate(string? userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedException("User authentication required");
            }
            if (!Guid.TryParse(userId, out var id))
            {
                throw new UnauthorizedException("User id not correct");
            }

            return id;
        }
    }
}
