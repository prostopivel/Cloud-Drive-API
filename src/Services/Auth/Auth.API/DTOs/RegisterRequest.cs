namespace Auth.API.DTOs
{
    public record RegisterRequest(
        string Username,
        string Email,
        string Password);
}
