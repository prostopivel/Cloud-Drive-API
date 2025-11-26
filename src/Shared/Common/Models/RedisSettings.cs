namespace Shared.Common.Models
{
    public class RedisSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string InstanceName { get; set; } = string.Empty;
        public int CacheTimeoutMinutes { get; set; } = 30;
    }
}
