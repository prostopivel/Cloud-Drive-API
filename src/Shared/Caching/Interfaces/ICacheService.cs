namespace Shared.Caching.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken token = default);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken token = default);
        Task RemoveAsync(string key, CancellationToken token = default);
        Task<bool> ExistsAsync(string key, CancellationToken token = default);
    }
}
