using System.Net;
using PayTR.PosSelection.Infrastructure.Interfaces.Redis;
using PayTR.PosSelection.Infrastructure.Models.Exceptions;
using Polly;
using Polly.CircuitBreaker;
using StackExchange.Redis;

namespace PayTR.PosSelection.Infrastructure.Services
{
    public class RedisClient : IRedisClient
    {
        private readonly IDatabase _db;
        private readonly IServer _server;
        private readonly IAsyncPolicy _circuitBreaker;

        public RedisClient(IConnectionMultiplexer mux, IAsyncPolicy circuitBreaker)
        {
            try
            {
                _db = mux.GetDatabase();
                _server = mux.GetServer(mux.GetEndPoints().First());
            } 
            catch (RedisException e)
            {
                throw new CustomErrorException("One of the system sub-displays is temporarily unavailable. Please try again later.", e);
            }
            _circuitBreaker = circuitBreaker;
        }
        
        public async Task<bool> Set(string key, string value)
        {
            try
            {
                return await _circuitBreaker.ExecuteAsync(async () =>
                {
                    var acquired= await _db.StringSetAsync(key, value);

                    return acquired;
                });
            }
            catch (BrokenCircuitException ex)
            { 
                throw new CustomErrorException("One of the system sub-displays is temporarily unavailable. Please try again later.", ex);
            }
            catch (RedisException e)
            {
                throw new CustomErrorException("One of the system sub-displays is temporarily unavailable. Please try again later.", e);
            }
          
        }

        public async Task<bool> Set(string key, string value, TimeSpan expiration, When when)
        {
            try
            {
                return await _circuitBreaker.ExecuteAsync(async () =>
                {
                    var acquired=  await _db.StringSetAsync(
                        key,
                        value,
                        expiry: expiration,
                        when);
                
                    return acquired;
                });
            }
            catch (BrokenCircuitException ex)
            { 
                throw new CustomErrorException("One of the system sub-displays is temporarily unavailable. Please try again later.", ex);
            }
            catch (RedisException e)
            {
                throw new CustomErrorException("One of the system sub-displays is temporarily unavailable. Please try again later.", e);
            }
        }

        public async Task<string?> Get(string key)
        {
            try
            {
                return await _circuitBreaker.ExecuteAsync(async () =>
                {
                    var value = await _db.StringGetAsync(key);

                    return value.HasValue ? value.ToString() : null;
                });
            }
            catch (BrokenCircuitException ex)
            { 
                throw new CustomErrorException("One of the system sub-displays is temporarily unavailable. Please try again later.", ex);
            }
            catch (RedisException e)
            {
                throw new CustomErrorException("One of the system sub-displays is temporarily unavailable. Please try again later.", e);
            }
        }
        
        public async Task<bool> Delete(string key)
        {
            try
            {
                return await _circuitBreaker.ExecuteAsync(async () =>
                {
                    var value = await _db.KeyDeleteAsync(key);

                    return value;
                });
            }
            catch (BrokenCircuitException ex)
            { 
                throw new CustomErrorException("One of the system sub-displays is temporarily unavailable. Please try again later.", ex);
            }
            catch (RedisException e)
            {
                throw new CustomErrorException("One of the system sub-displays is temporarily unavailable. Please try again later.", e);
            }
        }
        
        public async Task<bool> DeleteAllKey(string key)
        {
            try
            {
                return await _circuitBreaker.ExecuteAsync(async () =>
                {
                    var keys = _server.Keys(pattern: key);

                    foreach (var key in keys)
                    {
                        await Delete(key);
                    }
                    
                    return true;
                });
            }
            catch (BrokenCircuitException ex)
            { 
                throw new CustomErrorException("One of the system sub-displays is temporarily unavailable. Please try again later.", ex);
            }
            catch (RedisException e)
            {
                throw new CustomErrorException("One of the system sub-displays is temporarily unavailable. Please try again later.", e);
            }
        }
    }
}

