using System.Text.Json;
using ArticlesRepository.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ArticlesRepository.Infrastructure.Cache;

/// <summary>Cache fail-safe: qualquer falha do Redis é logada como warning e a requisição segue sem cache.</summary>
public class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int BatchSize = 500;

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        try
        {
            var value = await _redis.GetDatabase().StringGetAsync(key);
            return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>((string)value!, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao ler do cache (chave {CacheKey})", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        try
        {
            await _redis.GetDatabase().StringSetAsync(key, JsonSerializer.Serialize(value, JsonOptions), ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao gravar no cache (chave {CacheKey})", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct)
    {
        try
        {
            await _redis.GetDatabase().KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao remover do cache (chave {CacheKey})", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct)
    {
        try
        {
            var db = _redis.GetDatabase();
            foreach (var endpoint in _redis.GetEndPoints())
            {
                var server = _redis.GetServer(endpoint);
                if (!server.IsConnected || server.IsReplica)
                {
                    continue;
                }

                var batch = new List<RedisKey>(BatchSize);
                // Keys() usa SCAN quando o servidor suporta (não bloqueia como KEYS).
                foreach (var key in server.Keys(pattern: $"{prefix}*", pageSize: BatchSize))
                {
                    batch.Add(key);
                    if (batch.Count >= BatchSize)
                    {
                        await db.KeyDeleteAsync(batch.ToArray());
                        batch.Clear();
                    }
                }

                if (batch.Count > 0)
                {
                    await db.KeyDeleteAsync(batch.ToArray());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao invalidar o cache por prefixo {Prefix}", prefix);
        }
    }
}
