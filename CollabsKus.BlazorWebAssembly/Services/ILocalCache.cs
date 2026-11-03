namespace CollabsKus.BlazorWebAssembly.Services;

public interface ILocalCache
{
   ValueTask<CacheEntry<T>?> GetAsync<T>(string key);

   ValueTask<bool> SetAsync<T>(string key, T value);

   ValueTask RemoveAsync(string key);
}
