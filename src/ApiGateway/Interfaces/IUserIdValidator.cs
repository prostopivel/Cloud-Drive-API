namespace ApiGateway.Interfaces
{
    public interface IUserIdValidator
    {
        public Guid Validate(string? userId);
    }
}
