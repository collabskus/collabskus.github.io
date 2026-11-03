using System.Net.Http.Json;
using CollabsKus.BlazorWebAssembly.Models;
using CollabsKus.BlazorWebAssembly.Settings;
using Microsoft.Extensions.Options;

namespace CollabsKus.BlazorWebAssembly.Services;

public sealed class KathmanduCalendarService
{
   private const string CalendarCacheKey = "calendar";
   private const string ClockOffsetCacheKey = "clock-offset";

   private readonly HttpClient _httpClient;
   private readonly IApiLogger _logger;
   private readonly ILocalCache _cache;
   private readonly TimeProvider _timeProvider;
   private readonly CalendarApiOptions _api;
   private readonly TimeSpan _expectedOffset;

   private TimeSpan _clockOffset;
   private CacheEntry<CalendarCacheRecord>? _calendar;
   private bool _calendarFromNetwork;

   public KathmanduCalendarService(
      HttpClient httpClient,
      IApiLogger logger,
      ILocalCache cache,
      TimeProvider timeProvider,
      IOptions<CalendarApiOptions> apiOptions,
      IOptions<LocationOptions> locationOptions)
   {
      _httpClient = httpClient;
      _logger = logger;
      _cache = cache;
      _timeProvider = timeProvider;
      _api = apiOptions.Value;
      _expectedOffset = locationOptions.Value.UtcOffset;
      _clockOffset = _expectedOffset;
   }

   public DateTimeOffset? LastClockSync { get; private set; }

   public DateTime GetCurrentKathmanduTime() => _timeProvider.GetUtcNow().UtcDateTime + _clockOffset;

   public DateOnly GetKathmanduToday() => DateOnly.FromDateTime(GetCurrentKathmanduTime());

   public async Task<bool> SyncClockAsync(CancellationToken cancellationToken = default)
   {
      DateTimeOffset now = _timeProvider.GetUtcNow();
      if (IsClockOffsetFresh(LastClockSync, now))
      {
         return true;
      }

      CacheEntry<long>? cached = await _cache.GetAsync<long>(ClockOffsetCacheKey);
      if (cached is not null && IsClockOffsetFresh(cached.StoredAt, now))
      {
         ApplyClockOffset(TimeSpan.FromTicks(cached.Value), cached.StoredAt);
         _logger.LogApiRequest(_api.TimeUrl, new { offsetMinutes = _clockOffset.TotalMinutes }, fromCache: true);
         return true;
      }

      TimeSpan? fetched = await FetchClockOffsetAsync(cancellationToken);
      if (fetched is null)
      {
         return false;
      }

      ApplyClockOffset(fetched.Value, now);
      await _cache.SetAsync(ClockOffsetCacheKey, _clockOffset.Ticks);
      return true;
   }

   public async Task<CalendarSnapshot?> GetCalendarAsync(CancellationToken cancellationToken = default)
   {
      DateOnly today = GetKathmanduToday();
      CacheEntry<CalendarCacheRecord>? stored = await GetStoredCalendarAsync();

      if (stored is not null && stored.Value.ForDate == today)
      {
         CalendarDataSource source = _calendarFromNetwork ? CalendarDataSource.Network : CalendarDataSource.Cache;
         return new CalendarSnapshot(stored.Value.Response.Res, today, source, stored.StoredAt);
      }

      CacheEntry<CalendarCacheRecord>? fetched = await FetchCalendarAsync(today, cancellationToken);
      if (fetched is not null)
      {
         return new CalendarSnapshot(fetched.Value.Response.Res, today, CalendarDataSource.Network, fetched.StoredAt);
      }

      return BuildFallback(stored, today);
   }

   private static CalendarSnapshot? BuildFallback(CacheEntry<CalendarCacheRecord>? stored, DateOnly today)
   {
      if (stored is null)
      {
         return null;
      }

      CalendarCacheRecord record = stored.Value;
      CalendarData? projected = CalendarProjection.TryProject(record.Response.Res, record.ForDate, today);

      return projected is not null
         ? new CalendarSnapshot(projected, today, CalendarDataSource.Projected, stored.StoredAt)
         : new CalendarSnapshot(CalendarProjection.WithoutToday(record.Response.Res), record.ForDate, CalendarDataSource.Stale, stored.StoredAt);
   }

   private async ValueTask<CacheEntry<CalendarCacheRecord>?> GetStoredCalendarAsync()
   {
      if (_calendar is not null)
      {
         return _calendar;
      }

      CacheEntry<CalendarCacheRecord>? entry = await _cache.GetAsync<CalendarCacheRecord>(CalendarCacheKey);
      if (entry is null || !HasDays(entry.Value.Response))
      {
         return null;
      }

      _calendar = entry;
      _calendarFromNetwork = false;
      _logger.LogApiRequest(_api.TodayUrl, entry.Value.Response, fromCache: true);
      return entry;
   }

   private async Task<CacheEntry<CalendarCacheRecord>?> FetchCalendarAsync(DateOnly today, CancellationToken cancellationToken)
   {
      using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      timeout.CancelAfter(_api.RequestTimeout);

      try
      {
         CalendarResponse? response = await _httpClient.GetFromJsonAsync<CalendarResponse>(_api.TodayUrl, timeout.Token);
         if (response is null || !HasDays(response))
         {
            _logger.LogApiRequest(_api.TodayUrl, new { error = "Calendar response contained no days", failed = true }, fromCache: false);
            return null;
         }

         var record = new CalendarCacheRecord { ForDate = today, Response = response };
         var entry = new CacheEntry<CalendarCacheRecord>(record, _timeProvider.GetUtcNow());
         _calendar = entry;
         _calendarFromNetwork = true;
         await _cache.SetAsync(CalendarCacheKey, record);
         _logger.LogApiRequest(_api.TodayUrl, response, fromCache: false);
         return entry;
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
         throw;
      }
      catch (Exception ex)
      {
         _logger.LogApiRequest(_api.TodayUrl, new { error = ex.Message, failed = true }, fromCache: false);
         return null;
      }
   }

   private async Task<TimeSpan?> FetchClockOffsetAsync(CancellationToken cancellationToken)
   {
      using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      timeout.CancelAfter(_api.RequestTimeout);

      try
      {
         DateTime requestStartUtc = _timeProvider.GetUtcNow().UtcDateTime;
         TimeResponse? response = await _httpClient.GetFromJsonAsync<TimeResponse>(_api.TimeUrl, timeout.Token);
         DateTime responseUtc = _timeProvider.GetUtcNow().UtcDateTime;

         TimeSpan? offset = response is null
            ? null
            : ClockOffsetCalculator.Calculate(response, requestStartUtc, responseUtc, _expectedOffset);

         if (offset is null)
         {
            _logger.LogApiRequest(_api.TimeUrl, new { error = "Time response could not be parsed", failed = true }, fromCache: false);
            return null;
         }

         _logger.LogApiRequest(_api.TimeUrl, response, fromCache: false);
         return offset;
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
         throw;
      }
      catch (Exception ex)
      {
         _logger.LogApiRequest(_api.TimeUrl, new { error = ex.Message, failed = true }, fromCache: false);
         return null;
      }
   }

   private void ApplyClockOffset(TimeSpan offset, DateTimeOffset syncedAt)
   {
      _clockOffset = ClockOffsetCalculator.Normalize(offset, _expectedOffset);
      LastClockSync = syncedAt;
   }

   private bool IsClockOffsetFresh(DateTimeOffset? syncedAt, DateTimeOffset now) =>
      syncedAt is { } at && now >= at && now - at < _api.ClockOffsetCacheDuration;

   private static bool HasDays(CalendarResponse? response) => response?.Res?.Days is { Count: > 0 };

   private sealed class CalendarCacheRecord
   {
      public DateOnly ForDate { get; set; }

      public CalendarResponse Response { get; set; } = new();
   }
}
