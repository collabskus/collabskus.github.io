namespace CollabsKus.BlazorWebAssembly.Services;

public interface IKeyValueStore
{
   ValueTask<string?> GetAsync(string key);

   ValueTask<bool> SetAsync(string key, string value);

   ValueTask RemoveAsync(string key);
}
