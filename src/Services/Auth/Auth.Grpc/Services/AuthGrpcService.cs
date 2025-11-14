using Auth.Core.Interfaces.Services;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Auth.Grpc.Services
{
    public class AuthGrpcService : AuthService.AuthServiceBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthGrpcService> _logger;

        public AuthGrpcService(IAuthService authService, ILogger<AuthGrpcService> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public override async Task<TokenValidationResponse> ValidateToken(
            TokenRequest request,
            ServerCallContext context)
        {
            try
            {
                var token = context.CancellationToken;
                var isValid = await _authService.ValidateTokenAsync(
                    request.Token, token: token);
                Guid? userId = null;
                string username = string.Empty;

                if (isValid)
                {
                    userId = await _authService.GetUserIdFromTokenAsync(
                        request.Token, token: token);
                }

                return new TokenValidationResponse
                {
                    IsValid = isValid,
                    UserId = userId?.ToString()
                        ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return new TokenValidationResponse { IsValid = false };
            }
        }

        public override async Task<UserIdResponse> GetUserId(
            TokenRequest request,
            ServerCallContext context)
        {
            try
            {
                var token = context.CancellationToken;
                var userId = await _authService.GetUserIdFromTokenAsync(
                    request.Token, token: token);

                return new UserIdResponse
                {
                    UserId = userId?.ToString()
                        ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user ID from token");
                return new UserIdResponse();
            }
        }
    }
}
