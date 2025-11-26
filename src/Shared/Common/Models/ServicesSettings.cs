namespace Shared.Common.Models
{
    public class ServicesSettings
    {
        public string Auth { get; set; } = string.Empty;
        public string AuthGrpc {  get; set; } = string.Empty;
        public string FileMetadata { get; set; } = string.Empty;
        public string FileStorage { get; set; } = string.Empty;
    }
}
