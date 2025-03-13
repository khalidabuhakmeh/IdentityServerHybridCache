using Duende.IdentityServer.Services;

namespace HybridCacheSample.Caches;

public class NoOpCache<T> : ICache<T> 
    where T : class
{
    public NoOpCache()
    {
        Console.WriteLine("hello");
    }
    
    public Task<T?> GetAsync(string key)
    {
        return Task.FromResult<T?>(null);
    }

    public async Task<T> GetOrAddAsync(string key, TimeSpan duration, Func<Task<T>> get)
    {
        return await get();
    }

    public Task SetAsync(string key, T item, TimeSpan expiration)
    {
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        return Task.CompletedTask;
    }
}