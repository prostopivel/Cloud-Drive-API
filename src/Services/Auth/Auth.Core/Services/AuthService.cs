using Auth.Core.Entities;
using Auth.Core.Exceptions;
using Auth.Core.Interfaces.Repositories;
using Auth.Core.Interfaces.Services;
using Shared.Common.Exceptions;

namespace Auth.Core.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher _passwordHasher;

        public AuthService(IUserRepository userRepository,
            ITokenService tokenService,
            IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
        }

        public async Task<User> RegisterAsync(string username,
            string email,
            string password)
        {
            if (await _userRepository.ExistsByEmailAsync(email))
            {
                throw new ConflictException("Email already exists");
            }

            var passwordHash = _passwordHasher.Hash(password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            return await _userRepository.AddAsync(user);
        }

        public async Task<User> LoginAsync(string email,
            string password)
        {
            var user = await _userRepository.GetByEmailAsync(email)
                ?? throw new UnauthorizedException("Invalid credentials");

            if (!_passwordHasher.Verify(password, user.PasswordHash))
            {
                throw new UnauthorizedException("Invalid credentials");
            }

            return user;
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
            var (isValid, _) = _tokenService.ValidateToken(token);
            return Task.FromResult(isValid);
        }

        public Task<Guid?> GetUserIdFromTokenAsync(string token)
        {
            var (isValid, userId) = _tokenService.ValidateToken(token);
            return Task.FromResult(isValid ? userId : null);
        }
    }
}
