using CollabsKus.BlazorWebAssembly.Settings;

namespace CollabsKus.Tests.Settings;

public class OptionsValidationTests
{
   [Test]
   [Arguments(91.0, 85.0)]
   [Arguments(-91.0, 85.0)]
   [Arguments(27.0, 181.0)]
   public async Task Location_OutOfRangeCoordinates_AreRejected(double latitude, double longitude)
   {
      var options = new LocationOptions { Name = "X", Latitude = latitude, Longitude = longitude, UtcOffsetMinutes = 0 };
      await Assert.That(options.Validate().Any()).IsTrue();
   }

   [Test]
   public async Task Location_MissingName_IsRejected()
   {
      var options = new LocationOptions { Latitude = 0, Longitude = 0 };
      await Assert.That(options.Validate().Any()).IsTrue();
   }

   [Test]
   public async Task CalendarApi_RelativeUrl_IsRejected()
   {
      var options = new CalendarApiOptions
      {
         TodayUrl = "api/today",
         TimeUrl = "https://example.com/time",
         RequestTimeoutSeconds = 10,
         ClockOffsetCacheMinutes = 60
      };
      await Assert.That(options.Validate().Count()).IsEqualTo(1);
   }

   [Test]
   public async Task Refresh_ZeroTick_IsRejected()
   {
      var options = new RefreshOptions { ClockTickMilliseconds = 0, ClockSyncCheckMinutes = 15, CalendarCheckMinutes = 15 };
      await Assert.That(options.Validate().Any()).IsTrue();
   }

   [Test]
   public async Task Blog_AbsolutePath_IsRejected()
   {
      var options = new BlogOptions
      {
         ManifestPath = "/blog/manifest.json",
         PostsPath = "blog/posts",
         AuthorsPath = "blog/authors",
         ManifestCacheMinutes = 5
      };
      await Assert.That(options.Validate().Count()).IsEqualTo(1);
   }

   [Test]
   public async Task ApiLogger_Disabled_SkipsValidation()
   {
      var options = new ApiLoggerOptions { Enabled = false };
      await Assert.That(options.Validate().Any()).IsFalse();
   }

   [Test]
   public async Task Geolocation_Disabled_SkipsValidation()
   {
      var options = new GeolocationOptions { Enabled = false };
      await Assert.That(options.Validate().Any()).IsFalse();
   }

   [Test]
   public async Task LocalStorage_ZeroSchemaVersion_IsRejected()
   {
      var options = new LocalStorageOptions { KeyPrefix = "x:", SchemaVersion = 0 };
      await Assert.That(options.Validate().Any()).IsTrue();
   }

   [Test]
   public async Task Validator_PrefixesFailuresWithSectionName()
   {
      var result = new AppOptionsValidator<LocalStorageOptions>().Validate(null, new LocalStorageOptions());
      await Assert.That(result.Failed).IsTrue();
      await Assert.That(result.FailureMessage!.Contains("LocalStorage:")).IsTrue();
   }

   [Test]
   public async Task Validator_ValidOptions_Succeeds()
   {
      var result = new AppOptionsValidator<LocalStorageOptions>().Validate(null, new LocalStorageOptions { KeyPrefix = "x:", SchemaVersion = 1 });
      await Assert.That(result.Succeeded).IsTrue();
   }
}
