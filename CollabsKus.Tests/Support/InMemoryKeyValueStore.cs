using CollabsKus.BlazorWebAssembly.Services;

namespace CollabsKus.Tests.Support;

internal sealed class InMemoryKeyValueStore : IKeyValueStore
{
   private readonly Dictionary<string, string> _items = new(StringComparer.Ordinal);

   public IReadOnlyDictionary<string, string> Items => _items;

   public ValueTask<string?> GetAsync(string key) =>
      ValueTask.FromResult(_items.TryGetValue(key, out string? value) ? value : null);

   public ValueTask<bool> SetAsync(string key, string value)
   {
      _items[key] = value;
      return ValueTask.FromResult(true);
   }

   public ValueTask RemoveAsync(string key)
   {
      _items.Remove(key);
      return ValueTask.CompletedTask;
   }
}
