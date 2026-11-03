using CollabsKus.BlazorWebAssembly.Services;
using CollabsKus.BlazorWebAssembly.Settings;
using CollabsKus.Tests.Support;
using Microsoft.Extensions.Options;

namespace CollabsKus.Tests.Services;

public class LocalCacheTests
{
   private const string Prefix = "test:";
   private static readonly DateTimeOffset Now = new(2026, 4, 14, 6, 0, 0, TimeSpan.Zero);

   [Test]
   public async Task SetThenGet_ReturnsValueAndTimestamp()
   {
      var store = new InMemoryKeyValueStore();
      var cache = CreateCache(store, new ManualTimeProvider(Now));

      await cache.SetAsync("answer", 42L);
      var entry = await cache.GetAsync<long>("answer");

      await Assert.That(entry is null).IsFalse();
      await Assert.That(entry!.Value).IsEqualTo(42L);
      await Assert.That(entry.StoredAt).IsEqualTo(Now);
   }

   [Test]
   public async Task Set_UsesConfiguredKeyPrefix()
   {
      var store = new InMemoryKeyValueStore();
      var cache = CreateCache(store, new ManualTimeProvider(Now));

      await cache.SetAsync("key", "value");

      await Assert.That(store.Items.ContainsKey(Prefix + "key")).IsTrue();
   }

   [Test]
   public async Task Get_MissingKey_ReturnsNull()
   {
      var cache = CreateCache(new InMemoryKeyValueStore(), new ManualTimeProvider(Now));
      var entry = await cache.GetAsync<string>("missing");
      await Assert.That(entry is null).IsTrue();
   }

   [Test]
   public async Task Get_DifferentSchemaVersion_ReturnsNullAndRemovesEntry()
   {
      var store = new InMemoryKeyValueStore();
      await CreateCache(store, new ManualTimeProvider(Now), schemaVersion: 1).SetAsync("key", "value");

      var entry = await CreateCache(store, new ManualTimeProvider(Now), schemaVersion: 2).GetAsync<string>("key");

      await Assert.That(entry is null).IsTrue();
      await Assert.That(store.Items.ContainsKey(Prefix + "key")).IsFalse();
   }

   [Test]
   public async Task Get_CorruptJson_ReturnsNullAndRemovesEntry()
   {
      var store = new InMemoryKeyValueStore();
      await store.SetAsync(Prefix + "key", "{ not json");

      var entry = await CreateCache(store, new ManualTimeProvider(Now)).GetAsync<string>("key");

      await Assert.That(entry is null).IsTrue();
      await Assert.That(store.Items.ContainsKey(Prefix + "key")).IsFalse();
   }

   [Test]
   public async Task Remove_DeletesEntry()
   {
      var store = new InMemoryKeyValueStore();
      var cache = CreateCache(store, new ManualTimeProvider(Now));
      await cache.SetAsync("key", "value");

      await cache.RemoveAsync("key");

      await Assert.That(store.Items.Count).IsEqualTo(0);
   }

   private static LocalCache CreateCache(InMemoryKeyValueStore store, TimeProvider time, int schemaVersion = 1) =>
      new(store, time, Options.Create(new LocalStorageOptions { KeyPrefix = Prefix, SchemaVersion = schemaVersion }));
}
