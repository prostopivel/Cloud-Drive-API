using Auth.Core.Interfaces.Services;

namespace Auth.Core.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool Verify(string password, string comparePassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, comparePassword);
        }
    }
}
