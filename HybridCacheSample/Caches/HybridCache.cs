using System.Diagnostics;
using Duende.IdentityServer.Services;
using Microsoft.Extensions.Caching.Hybrid;

namespace HybridCacheSample.Caches;

public class HybridCache<T>(HybridCache cache, ILogger<HybridCache<T>> logger) : ICache<T>
    where T : class
{
    private static string GetKey(string key) =>
        $"/{typeof(T).FullName}/{key}";
    
    public async Task<T?> GetAsync(string key)
    {
        using var activity = Activity.Current?.Source.StartActivity();

        // this is a get, so expire immediately
        var result = await cache.GetOrCreateAsync<T?>(GetKey(key),
            _ => default,
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.Zero,
                LocalCacheExpiration = TimeSpan.Zero,
                Flags = HybridCacheEntryFlags.DisableLocalCache | HybridCacheEntryFlags.DisableDistributedCache
            });
        
        return result;
    }

    public async Task<T> GetOrAddAsync(string key, TimeSpan duration, Func<Task<T>> factory)
    {
        using var activity = Activity.Current?.Source.StartActivity();
        
        logger.LogInformation("Get or Add for {Type}: {Key}", typeof(T).FullName, key);
        
        return await cache.GetOrCreateAsync<Func<Task<T>>, T>(
            GetKey(key), 
            factory, 
            async (fact, _) =>
            {
                logger.LogInformation("creating for {Type}: {Key}", typeof(T).FullName, key);
                return await fact();
            });
    }

    public async Task SetAsync(string key, T item, TimeSpan expiration)
    {
        using var activity = Activity.Current?.Source.StartActivity();
        logger.LogInformation("cache set for {Type}: {Key}", typeof(T).FullName, key);
        
        await cache.SetAsync(GetKey(key), item, new()
        {
            Expiration = expiration,
            LocalCacheExpiration = expiration
        });
    }

    public async Task RemoveAsync(string key)
    {
        using var activity = Activity.Current?.Source.StartActivity();
        logger.LogInformation("cache remove for {Type}: {Key}", typeof(T).FullName, key);
        
        await cache.RemoveAsync(GetKey(key));
    }
}