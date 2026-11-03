namespace CollabsKus.BlazorWebAssembly.Services;

public sealed record CacheEntry<T>(T Value, DateTimeOffset StoredAt);
