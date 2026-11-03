using CollabsKus.BlazorWebAssembly.Settings;

namespace CollabsKus.Tests.Settings;

public class TelemetryOptionsTests
{
   [Test]
   public async Task IsEnabled_NullConnectionString_ReturnsFalse()
   {
      var options = new TelemetryOptions { ConnectionString = null };
      await Assert.That(options.IsEnabled).IsFalse();
   }

   [Test]
   [Arguments("")]
   [Arguments("   ")]
   public async Task IsEnabled_BlankConnectionString_ReturnsFalse(string connectionString)
   {
      var options = new TelemetryOptions { ConnectionString = connectionString };
      await Assert.That(options.IsEnabled).IsFalse();
   }

   [Test]
   public async Task IsEnabled_ConnectionStringPresent_ReturnsTrue()
   {
      var options = new TelemetryOptions { ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000" };
      await Assert.That(options.IsEnabled).IsTrue();
   }

   [Test]
   public async Task Validate_Disabled_HasNoErrors()
   {
      var options = new TelemetryOptions();
      await Assert.That(options.Validate().Any()).IsFalse();
   }

   [Test]
   public async Task Validate_EnabledWithoutSdkUrl_ReportsError()
   {
      var options = new TelemetryOptions
      {
         ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000",
         CloudRole = "role",
         MaxQueueLength = 10
      };
      await Assert.That(options.Validate().Count()).IsEqualTo(1);
   }

   [Test]
   public async Task Environment_DefaultsToProduction()
   {
      await Assert.That(new TelemetryOptions().Environment).IsEqualTo(TelemetryOptions.DefaultEnvironment);
   }

   [Test]
   public async Task SectionName_MatchesAppSettings()
   {
      await Assert.That(TelemetryOptions.SectionName).IsEqualTo("ApplicationInsights");
   }
}
