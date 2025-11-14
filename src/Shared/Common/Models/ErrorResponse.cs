namespace Shared.Common.Models
{
    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public object? Details { get; set; }
    }
}