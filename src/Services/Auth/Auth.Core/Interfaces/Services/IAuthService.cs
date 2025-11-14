using Auth.Core.Entities;

namespace Auth.Core.Interfaces.Services
{
    public interface IAuthService
    {
        Task<User> RegisterAsync(string username, string email, string password);
        Task<User> LoginAsync(string username, string password);
        Task<bool> ValidateTokenAsync(string token);
        Task<Guid?> GetUserIdFromTokenAsync(string token);
    }
}
