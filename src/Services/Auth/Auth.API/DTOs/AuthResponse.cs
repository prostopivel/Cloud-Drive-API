namespace Auth.API.DTOs
{
    public record AuthResponse
    {
        public Guid UserId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Token { get; init; } = string.Empty;
    }
}
