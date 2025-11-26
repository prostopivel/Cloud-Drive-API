using Auth.Core.Entities;

namespace Auth.Core.Interfaces.Services
{
    public interface IAuthService
    {
        Task<User> RegisterAsync(string username, string email, string password, CancellationToken token = default);
        Task<User> LoginAsync(string username, string password, CancellationToken token = default);
        Task<bool> ValidateTokenAsync(string jwtToken, CancellationToken token = default);
        Task<Guid?> GetUserIdFromTokenAsync(string jwtToken, CancellationToken token = default);
    }
}
