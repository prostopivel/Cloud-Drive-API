using Auth.API.DTOs;
using Auth.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Auth.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;

        public AuthController(IAuthService authService, ITokenService tokenService)
        {
            _authService = authService;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var user = await _authService.RegisterAsync(
                request.Username,
                request.Email,
                request.Password);
            var token = _tokenService.GenerateToken(user);

            return Ok(new AuthResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Token = token
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _authService.LoginAsync(
                request.Email,
                request.Password);

            var token = _tokenService.GenerateToken(user);

            return Ok(new AuthResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Token = token
            });
        }

        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ValidateTokenRequest request)
        {
            var isValid = await _authService.ValidateTokenAsync(request.Token);
            return Ok(new { isValid });
        }
    }
}
