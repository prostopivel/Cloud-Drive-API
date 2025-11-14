using Auth.Core.Entities;

namespace Auth.Core.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken token = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken token = default);
        Task<User> AddAsync(User user, CancellationToken token = default);
        Task<bool> ExistsByEmailAsync(string email, CancellationToken token = default);
    }
}
