using System.Text.Json;
using CollabsKus.BlazorWebAssembly.Settings;
using Microsoft.Extensions.Options;

namespace CollabsKus.BlazorWebAssembly.Services;

public sealed class LocalCache(IKeyValueStore store, TimeProvider timeProvider, IOptions<LocalStorageOptions> options) : ILocalCache
{
   private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

   private readonly LocalStorageOptions _options = options.Value;

   public async ValueTask<CacheEntry<T>?> GetAsync<T>(string key)
   {
      string fullKey = BuildKey(key);
      string? json = await store.GetAsync(fullKey);
      if (string.IsNullOrEmpty(json))
      {
         return null;
      }

      Envelope<T>? envelope;
      try
      {
         envelope = JsonSerializer.Deserialize<Envelope<T>>(json, SerializerOptions);
      }
      catch (JsonException)
      {
         envelope = null;
      }

      if (envelope is null || envelope.Version != _options.SchemaVersion || envelope.Value is null)
      {
         await store.RemoveAsync(fullKey);
         return null;
      }

      return new CacheEntry<T>(envelope.Value, envelope.StoredAt);
   }

   public async ValueTask<bool> SetAsync<T>(string key, T value)
   {
      var envelope = new Envelope<T>
      {
         Version = _options.SchemaVersion,
         StoredAt = timeProvider.GetUtcNow(),
         Value = value
      };

      string json;
      try
      {
         json = JsonSerializer.Serialize(envelope, SerializerOptions);
      }
      catch (Exception ex) when (ex is JsonException or NotSupportedException)
      {
         return false;
      }

      return await store.SetAsync(BuildKey(key), json);
   }

   public ValueTask RemoveAsync(string key) => store.RemoveAsync(BuildKey(key));

   private string BuildKey(string key) => _options.KeyPrefix + key;

   private sealed class Envelope<TValue>
   {
      public int Version { get; set; }

      public DateTimeOffset StoredAt { get; set; }

      public TValue? Value { get; set; }
   }
}
