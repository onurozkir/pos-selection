using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PayTR.PosSelection.API.Extensions;
using PayTR.PosSelection.Infrastructure.Factory;
using PayTR.PosSelection.Infrastructure.Factory.Interfaces;
using PayTR.PosSelection.Infrastructure.Interfaces.RationApiClient;
using PayTR.PosSelection.Infrastructure.Interfaces.PosRatios;
using PayTR.PosSelection.Infrastructure.Interfaces.PosSelection;
using PayTR.PosSelection.Infrastructure.Interfaces.PriceCalculator;
using PayTR.PosSelection.Infrastructure.Interfaces.Redis;
using PayTR.PosSelection.Infrastructure.Repositories;
using PayTR.PosSelection.Infrastructure.Services;
using StackExchange.Redis;

namespace PayTR.PosSelection.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfra(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var connString = configuration.GetConnectionString("Redis")
                             ?? configuration["Redis:ConnectionString"]
                             ?? "redis:6379";

            var options = ConfigurationOptions.Parse(connString);
            options.AbortOnConnectFail = false; 
            options.ConnectTimeout = 5000;
            options.SyncTimeout = 1000;
            options.AsyncTimeout = 1000;

            return ConnectionMultiplexer.Connect(options);
        });
        services.AddHttpClient("RatiosClient", client => { })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
        services.AddTransient<IRatiosApiClient, RatiosApiClient>();
        services.AddTransient<IRedisClient, RedisClient>();
        services.AddScoped<IPosRatios, PosRatios>();
        services.AddScoped<IPosSelection, Services.PosSelection>();
        services.AddSingleton<IPriceCalculator, TRYCalculator>();
        services.AddSingleton<IPriceCalculator, USDCalculator>();
        services.AddSingleton<IPriceCalculatorFactory, PriceCalculatorFactory>();
        services.AddCircuitBreakerPolicies();
        
        return services;
    }
}