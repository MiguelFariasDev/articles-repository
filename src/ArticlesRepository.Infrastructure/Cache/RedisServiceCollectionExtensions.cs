using ArticlesRepository.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ArticlesRepository.Infrastructure.Cache;

public static class RedisServiceCollectionExtensions
{
    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration["Redis:Connection"] ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(connection);
            options.AbortOnConnectFail = false; // cache fora do ar nunca impede a API de subir
            options.ConnectTimeout = 2000;
            options.AsyncTimeout = 1500;
            options.SyncTimeout = 1500;
            return ConnectionMultiplexer.Connect(options);
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }
}
