using Auth.Core.Entities;
using Auth.Core.Interfaces.Repositories;
using Auth.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AuthDbContext _context;

        public UserRepository(AuthDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id,
            CancellationToken token = default)
        {
            return await _context.Users.FindAsync([id], cancellationToken: token);
        }

        public async Task<User?> GetByEmailAsync(string email,
            CancellationToken token = default)
        {
            return await _context.Users
                .SingleOrDefaultAsync(u => u.Email == email, cancellationToken: token);
        }

        public async Task<User> AddAsync(User user,
            CancellationToken token = default)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken: token);
            return user;
        }

        public async Task<bool> ExistsByEmailAsync(string email,
            CancellationToken token = default)
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email, cancellationToken: token);
        }
    }
}
