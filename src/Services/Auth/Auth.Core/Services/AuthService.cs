using Auth.Core.Constants;
using Auth.Core.Entities;
using Auth.Core.Exceptions;
using Auth.Core.Interfaces.Repositories;
using Auth.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Shared.Common.Exceptions;

namespace Auth.Core.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICacheService _cacheService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository userRepository,
            ITokenService tokenService,
            IPasswordHasher passwordHasher,
            ICacheService cacheService,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<User> RegisterAsync(string username,
            string email,
            string password,
            CancellationToken token = default)
        {
            if (await _userRepository.ExistsByEmailAsync(email, token: token))
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

            var createdUser = await _userRepository.AddAsync(user, token: token);

            await InvalidateUserCache(createdUser.Id, createdUser.Email, token: token);

            return createdUser;
        }

        public async Task<User> LoginAsync(string email,
            string password,
            CancellationToken token = default)
        {
            var cacheKey = $"{CacheKeys.USER_BY_EMAIL}:{email}";
            var cachedUser = await _cacheService.GetAsync<User>(cacheKey, token: token);

            if (cachedUser != null)
            {
                _logger.LogInformation("User {Email} found in cache", email);
                if (!_passwordHasher.Verify(password, cachedUser.PasswordHash))
                {
                    throw new UnauthorizedException("Invalid credentials");
                }

                return cachedUser;
            }

            var user = await _userRepository.GetByEmailAsync(email, token: token)
                ?? throw new UnauthorizedException("Invalid credentials");

            if (!_passwordHasher.Verify(password, user.PasswordHash))
            {
                throw new UnauthorizedException("Invalid credentials");
            }

            await _cacheService.SetAsync(cacheKey, user,
                TimeSpan.FromMinutes(30), token: token);
            _logger.LogInformation("User {Email} cached", email);

            return user;
        }

        public async Task<bool> ValidateTokenAsync(string jwtToken,
            CancellationToken token = default)
        {
            var cacheKey = $"{CacheKeys.TOKEN_VALIDATION}:{jwtToken}";
            var cachedResult = await _cacheService.GetAsync<bool?>(cacheKey, token: token);

            if (cachedResult.HasValue)
            {
                _logger.LogDebug("Token validation result found in cache: {IsValid}", cachedResult.Value);
                return cachedResult.Value;
            }

            var (isValid, _) = _tokenService.ValidateToken(jwtToken);

            var cacheExpiry = isValid
                ? TimeSpan.FromMinutes(5)
                : TimeSpan.FromMinutes(1);
            await _cacheService.SetAsync(cacheKey, isValid, cacheExpiry, token: token);

            return isValid;
        }

        public async Task<Guid?> GetUserIdFromTokenAsync(string jwtToken,
            CancellationToken token = default)
        {
            var cacheKey = $"{CacheKeys.TOKEN_USER}:{jwtToken}";
            var cachedUserId = await _cacheService.GetAsync<string>(cacheKey, token: token);

            if (!string.IsNullOrEmpty(cachedUserId) && Guid.TryParse(cachedUserId, out var userId))
            {
                _logger.LogDebug("User ID from token found in cache: {UserId}", userId);
                return userId;
            }

            var (isValid, userIdFromToken) = _tokenService.ValidateToken(jwtToken);
            if (!isValid)
            {
                return null;
            }

            await _cacheService.SetAsync(cacheKey, userIdFromToken.ToString(),
                TimeSpan.FromMinutes(5), token: token);

            return userIdFromToken;
        }

        private async Task InvalidateUserCache(Guid userId,
            string email,
            CancellationToken token = default)
        {
            var tasks = new List<Task>
            {
                _cacheService.RemoveAsync($"{CacheKeys.USER_BY_ID}:{userId}", token: token),
                _cacheService.RemoveAsync($"{CacheKeys.USER_BY_EMAIL}:{email}", token: token)
            };

            await Task.WhenAll(tasks);
            _logger.LogInformation("User cache invalidated for user {UserId}", userId);
        }
    }
}
