using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Polly;
using StackExchange.Redis;

namespace PayTR.PosSelection.API.Extensions
{
    public static class CircuitBreakerExtension
    {
        public static IServiceCollection AddCircuitBreakerPolicies(this IServiceCollection services)
        {
            // 3 hata üstüste -> circuit OPEN
            // 2 m OPEN kal -> sonra HALF-OPEN
            // süreler test için olup doğru süreler için DORA metriklerine bakılmalı
            var circuitBreakerPolicy = Policy
                .Handle<TimeoutException>() // http timeout
                .Or<RedisConnectionException>()   // redis connection timeout 
                .Or<RedisTimeoutException>()   // redis timeout  
                .Or<NpgsqlException>(ex => ex.InnerException is TimeoutException)     // postgreSQl sadece Timeout hatası
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromMinutes(2),
                    onBreak: (ex, breakDelay) =>
                    {
                        // ilgili takıma webhook ile mesaj yollandı
                        Console.WriteLine("Circuit is Open (down) and send message team");
                    },
                    onReset: () =>
                    {
                        Console.WriteLine("Circuit is Clsoed (normal)");
                    },
                    onHalfOpen: () =>
                    {
                        Console.WriteLine("Circuit is half-open");
                    });

            services.AddSingleton<IAsyncPolicy>(circuitBreakerPolicy);

            return services;
        }
    }
}

