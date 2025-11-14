using Auth.Core.Entities;

namespace Auth.Core.Interfaces.Services
{
    public interface ITokenService
    {
        string GenerateToken(User user);
        (bool isValid, Guid? userId) ValidateToken(string token);
    }
}
