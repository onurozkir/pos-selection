using StackExchange.Redis;

namespace PayTR.PosSelection.Infrastructure.Interfaces.Redis
{
    public interface IRedisClient
    {
        Task<bool> Set(string key, string value);
        Task<bool> Set(string key, string value, TimeSpan expiration, When when);
        Task<string?> Get(string key);
        Task<bool> Delete(string key);
        Task<bool> DeleteAllKey(string key);
    }
}

