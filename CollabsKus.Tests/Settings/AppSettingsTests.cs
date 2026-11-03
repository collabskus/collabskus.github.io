using CollabsKus.BlazorWebAssembly.Settings;
using Microsoft.Extensions.Configuration;

namespace CollabsKus.Tests.Settings;

public class AppSettingsTests
{
   private static readonly IConfiguration Configuration = new ConfigurationBuilder()
      .SetBasePath(AppContext.BaseDirectory)
      .AddJsonFile(Path.Combine("TestData", "appsettings.json"), optional: false)
      .Build();

   [Test]
   public async Task Location_IsValid() => await AssertValid<LocationOptions>();

   [Test]
   public async Task CalendarApi_IsValid() => await AssertValid<CalendarApiOptions>();

   [Test]
   public async Task Refresh_IsValid() => await AssertValid<RefreshOptions>();

   [Test]
   public async Task LocalStorage_IsValid() => await AssertValid<LocalStorageOptions>();

   [Test]
   public async Task Geolocation_IsValid() => await AssertValid<GeolocationOptions>();

   [Test]
   public async Task ApiLogger_IsValid() => await AssertValid<ApiLoggerOptions>();

   [Test]
   public async Task Blog_IsValid() => await AssertValid<BlogOptions>();

   [Test]
   public async Task Telemetry_IsValid() => await AssertValid<TelemetryOptions>();

   [Test]
   public async Task Location_IsKathmandu()
   {
      var location = Bind<LocationOptions>();
      await Assert.That(location.UtcOffset).IsEqualTo(TimeSpan.FromMinutes(345));
      await Assert.That(location.Name).IsEqualTo("Kathmandu, Nepal");
   }

   private static T Bind<T>() where T : class, IAppOptions =>
      Configuration.GetSection(T.SectionName).Get<T>()
      ?? throw new InvalidOperationException($"Section '{T.SectionName}' is missing from appsettings.json.");

   private static async Task AssertValid<T>() where T : class, IAppOptions
   {
      string errors = string.Join("; ", Bind<T>().Validate());
      await Assert.That(errors).IsEqualTo(string.Empty);
   }
}
