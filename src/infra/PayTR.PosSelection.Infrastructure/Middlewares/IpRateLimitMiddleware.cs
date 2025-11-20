using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PayTR.PosSelection.Infrastructure.Interfaces.Redis;
using PayTR.PosSelection.Infrastructure.Models.Exceptions;
using StackExchange.Redis;

namespace PayTR.PosSelection.Infrastructure.Middlewares
{
    public class IpRateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConnectionMultiplexer _redis;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<IpRateLimitMiddleware> _logger;
        private readonly IRedisClient _redisClient;

        // 10 saniyede 1 istek
        private readonly int _windowSeconds = 10;
        
        public IpRateLimitMiddleware(
            RequestDelegate next,
            IRedisClient redisClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<IpRateLimitMiddleware> logger)
        {
            _next = next;
            _redisClient = redisClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }
        
        public async Task InvokeAsync(HttpContext context)
        {
            
            if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }
             
            var httpContext = _httpContextAccessor.HttpContext ?? context;

            var ip = httpContext.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrWhiteSpace(ip))
            {
                 
                await _next(context);
                return;
            }

            var key = $"rate-limit:{ip}";

            // if exist kontrolü ve 10 sn lik ip rate uygulandı
            // burada  doğru zaman aralığı metrikler takip edilerek tespit edilebilir
            var acquired = await _redisClient.Set(
                key,
                "1",
                TimeSpan.FromSeconds(_windowSeconds),
                when: When.NotExists);

            if (!acquired)
            {
                // Key zaten var -> pencere içinde daha önce istek yapmış
                _logger.LogWarning("Rate limit exceeded for IP {Ip}", ip);
                throw new IpRateLimitException("Too many requests");
                return;
            }

            await _next(context);
        }
    }
}

