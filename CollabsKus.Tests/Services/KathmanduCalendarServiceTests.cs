using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using CollabsKus.BlazorWebAssembly.Models;
using CollabsKus.BlazorWebAssembly.Services;
using CollabsKus.BlazorWebAssembly.Settings;
using CollabsKus.Tests.Support;
using Microsoft.Extensions.Options;

namespace CollabsKus.Tests.Services;

public class KathmanduCalendarServiceTests
{
   private const string TodayUrl = "https://calendar.example/api/today";
   private const string TimeUrl = "https://calendar.example/api/time";
   private const int TodayIndex = 10;

   private static readonly DateTimeOffset Start = new(2026, 4, 14, 6, 0, 0, TimeSpan.Zero);
   private static readonly TimeSpan Skew = TimeSpan.FromSeconds(30);

   [Test]
   public async Task GetCurrentKathmanduTime_BeforeSync_UsesConfiguredOffset()
   {
      var time = new ManualTimeProvider(Start);
      var service = CreateService(StubHttpMessageHandler.Offline(), new InMemoryKeyValueStore(), time);

      await Assert.That(service.GetCurrentKathmanduTime()).IsEqualTo(Start.UtcDateTime + TestLocations.NepalOffset);
   }

   [Test]
   public async Task GetCalendarAsync_NoCache_FetchesFromNetworkAndStores()
   {
      var time = new ManualTimeProvider(Start);
      var store = new InMemoryKeyValueStore();
      var handler = OnlineHandler(time);
      var service = CreateService(handler, store, time);

      var snapshot = await service.GetCalendarAsync();

      await Assert.That(snapshot is null).IsFalse();
      await Assert.That(snapshot!.Source).IsEqualTo(CalendarDataSource.Network);
      await Assert.That(snapshot.ForDate).IsEqualTo(KathmanduDate(time));
      await Assert.That(handler.Requests.Count).IsEqualTo(1);
      await Assert.That(store.Items.Count).IsEqualTo(1);
   }

   [Test]
   public async Task GetCalendarAsync_SecondCall_UsesMemoryWithoutNetwork()
   {
      var time = new ManualTimeProvider(Start);
      var handler = OnlineHandler(time);
      var service = CreateService(handler, new InMemoryKeyValueStore(), time);

      await service.GetCalendarAsync();
      await service.GetCalendarAsync();

      await Assert.That(handler.Requests.Count).IsEqualTo(1);
   }

   [Test]
   public async Task GetCalendarAsync_StoredForToday_DoesNotCallNetwork()
   {
      var time = new ManualTimeProvider(Start);
      var store = new InMemoryKeyValueStore();
      await CreateService(OnlineHandler(time), store, time).GetCalendarAsync();

      var handler = OnlineHandler(time);
      var logger = new RecordingApiLogger();
      var snapshot = await CreateService(handler, store, time, logger).GetCalendarAsync();

      await Assert.That(snapshot!.Source).IsEqualTo(CalendarDataSource.Cache);
      await Assert.That(handler.Requests.Count).IsEqualTo(0);
      await Assert.That(logger.Entries.Any(e => e.Endpoint == TodayUrl && e.FromCache)).IsTrue();
   }

   [Test]
   public async Task GetCalendarAsync_NextDayOffline_ProjectsSavedCalendar()
   {
      var time = new ManualTimeProvider(Start);
      var store = new InMemoryKeyValueStore();
      await CreateService(OnlineHandler(time), store, time).GetCalendarAsync();
      DateOnly savedDate = KathmanduDate(time);

      time.Advance(TimeSpan.FromDays(1));
      var snapshot = await CreateService(StubHttpMessageHandler.Offline(), store, time).GetCalendarAsync();

      await Assert.That(snapshot!.Source).IsEqualTo(CalendarDataSource.Projected);
      await Assert.That(snapshot.ForDate).IsEqualTo(savedDate.AddDays(1));
      await Assert.That(CalendarProjection.FindTodayIndex(snapshot.Data)).IsEqualTo(TodayIndex + 1);
   }

   [Test]
   public async Task GetCalendarAsync_NextMonthOffline_ReturnsStaleWithoutToday()
   {
      var time = new ManualTimeProvider(Start);
      var store = new InMemoryKeyValueStore();
      await CreateService(OnlineHandler(time), store, time).GetCalendarAsync();
      DateOnly savedDate = KathmanduDate(time);

      time.Advance(TimeSpan.FromDays(CalendarFixtures.MonthDays));
      var snapshot = await CreateService(StubHttpMessageHandler.Offline(), store, time).GetCalendarAsync();

      await Assert.That(snapshot!.Source).IsEqualTo(CalendarDataSource.Stale);
      await Assert.That(snapshot.ForDate).IsEqualTo(savedDate);
      await Assert.That(CalendarProjection.FindTodayIndex(snapshot.Data)).IsEqualTo(-1);
   }

   [Test]
   public async Task GetCalendarAsync_NextDayOnline_FetchesFreshCalendar()
   {
      var time = new ManualTimeProvider(Start);
      var store = new InMemoryKeyValueStore();
      await CreateService(OnlineHandler(time), store, time).GetCalendarAsync();

      time.Advance(TimeSpan.FromDays(1));
      var handler = OnlineHandler(time);
      var snapshot = await CreateService(handler, store, time).GetCalendarAsync();

      await Assert.That(snapshot!.Source).IsEqualTo(CalendarDataSource.Network);
      await Assert.That(handler.Requests.Count).IsEqualTo(1);
   }

   [Test]
   public async Task GetCalendarAsync_NoCacheAndOffline_ReturnsNull()
   {
      var time = new ManualTimeProvider(Start);
      var logger = new RecordingApiLogger();
      var snapshot = await CreateService(StubHttpMessageHandler.Offline(), new InMemoryKeyValueStore(), time, logger).GetCalendarAsync();

      await Assert.That(snapshot is null).IsTrue();
      await Assert.That(logger.Entries.Count).IsEqualTo(1);
   }

   [Test]
   public async Task GetCalendarAsync_EmptyResponse_TreatedAsFailure()
   {
      var time = new ManualTimeProvider(Start);
      var handler = new StubHttpMessageHandler(_ => Json(new CalendarResponse()));
      var snapshot = await CreateService(handler, new InMemoryKeyValueStore(), time).GetCalendarAsync();

      await Assert.That(snapshot is null).IsTrue();
   }

   [Test]
   public async Task SyncClockAsync_Online_AppliesServerOffset()
   {
      var time = new ManualTimeProvider(Start);
      var service = CreateService(OnlineHandler(time), new InMemoryKeyValueStore(), time);

      bool synced = await service.SyncClockAsync();

      await Assert.That(synced).IsTrue();
      await Assert.That(service.GetCurrentKathmanduTime()).IsEqualTo(Start.UtcDateTime + TestLocations.NepalOffset + Skew);
      await Assert.That(service.LastClockSync).IsEqualTo(Start);
   }

   [Test]
   public async Task SyncClockAsync_FreshStoredOffset_DoesNotCallNetwork()
   {
      var time = new ManualTimeProvider(Start);
      var store = new InMemoryKeyValueStore();
      await CreateService(OnlineHandler(time), store, time).SyncClockAsync();

      time.Advance(TimeSpan.FromMinutes(10));
      var handler = StubHttpMessageHandler.Offline();
      var service = CreateService(handler, store, time);
      bool synced = await service.SyncClockAsync();

      await Assert.That(synced).IsTrue();
      await Assert.That(handler.Requests.Count).IsEqualTo(0);
      await Assert.That(service.GetCurrentKathmanduTime()).IsEqualTo(time.UtcNow.UtcDateTime + TestLocations.NepalOffset + Skew);
   }

   [Test]
   public async Task SyncClockAsync_ExpiredStoredOffsetAndOffline_FallsBackToConfiguredOffset()
   {
      var time = new ManualTimeProvider(Start);
      var store = new InMemoryKeyValueStore();
      await CreateService(OnlineHandler(time), store, time).SyncClockAsync();

      time.Advance(TimeSpan.FromMinutes(61));
      var handler = StubHttpMessageHandler.Offline();
      var service = CreateService(handler, store, time);
      bool synced = await service.SyncClockAsync();

      await Assert.That(synced).IsFalse();
      await Assert.That(handler.Requests.Count).IsEqualTo(1);
      await Assert.That(service.GetCurrentKathmanduTime()).IsEqualTo(time.UtcNow.UtcDateTime + TestLocations.NepalOffset);
   }

   [Test]
   public async Task SyncClockAsync_UnparseableResponse_ReturnsFalse()
   {
      var time = new ManualTimeProvider(Start);
      var handler = new StubHttpMessageHandler(_ => Json(new TimeResponse { Hour = "??", Min = "00", Sec = "00", ApOrPm = "AM" }));
      var service = CreateService(handler, new InMemoryKeyValueStore(), time);

      await Assert.That(await service.SyncClockAsync()).IsFalse();
      await Assert.That(service.LastClockSync.HasValue).IsFalse();
   }

   private static KathmanduCalendarService CreateService(
      StubHttpMessageHandler handler,
      InMemoryKeyValueStore store,
      ManualTimeProvider time,
      RecordingApiLogger? logger = null)
   {
      var cache = new LocalCache(store, time, Options.Create(new LocalStorageOptions { KeyPrefix = "test:", SchemaVersion = 1 }));
      return new KathmanduCalendarService(
         new HttpClient(handler),
         logger ?? new RecordingApiLogger(),
         cache,
         time,
         Options.Create(new CalendarApiOptions
         {
            TodayUrl = TodayUrl,
            TimeUrl = TimeUrl,
            RequestTimeoutSeconds = 5,
            ClockOffsetCacheMinutes = 60
         }),
         Options.Create(new LocationOptions
         {
            Name = TestLocations.Kathmandu.Name,
            Latitude = TestLocations.Kathmandu.Latitude,
            Longitude = TestLocations.Kathmandu.Longitude,
            UtcOffsetMinutes = (int)TestLocations.NepalOffset.TotalMinutes
         }));
   }

   private static StubHttpMessageHandler OnlineHandler(ManualTimeProvider time) => new(request =>
   {
      string url = request.RequestUri!.ToString();
      if (url == TodayUrl)
      {
         return Json(CalendarFixtures.CreateResponse(TodayIndex));
      }

      if (url == TimeUrl)
      {
         return Json(ToTimeResponse(time.UtcNow.UtcDateTime + TestLocations.NepalOffset + Skew));
      }

      return new HttpResponseMessage(HttpStatusCode.NotFound);
   });

   private static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

   private static TimeResponse ToTimeResponse(DateTime kathmanduTime)
   {
      int hour12 = kathmanduTime.Hour % 12 == 0 ? 12 : kathmanduTime.Hour % 12;
      return new TimeResponse
      {
         Hour = hour12.ToString(CultureInfo.InvariantCulture),
         Min = kathmanduTime.Minute.ToString("D2", CultureInfo.InvariantCulture),
         Sec = kathmanduTime.Second.ToString("D2", CultureInfo.InvariantCulture),
         ApOrPm = kathmanduTime.Hour < 12 ? "AM" : "PM"
      };
   }

   private static DateOnly KathmanduDate(ManualTimeProvider time) =>
      DateOnly.FromDateTime(time.UtcNow.UtcDateTime + TestLocations.NepalOffset);
}
