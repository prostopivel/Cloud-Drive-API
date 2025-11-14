namespace Auth.API.DTOs
{
    public record LoginRequest(
        string Email,
        string Password);
}
