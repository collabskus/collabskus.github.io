using CollabsKus.BlazorWebAssembly.Services;

namespace CollabsKus.Tests.Services;

public class TelemetryOptionsTests
{
   [Test]
   public async Task IsEnabled_NullConnectionString_ReturnsFalse()
   {
      var options = new TelemetryOptions(null, "Production");
      await Assert.That(options.IsEnabled).IsFalse();
   }

   [Test]
   [Arguments("")]
   [Arguments("   ")]
   public async Task IsEnabled_BlankConnectionString_ReturnsFalse(string connectionString)
   {
      var options = new TelemetryOptions(connectionString, "Production");
      await Assert.That(options.IsEnabled).IsFalse();
   }

   [Test]
   public async Task IsEnabled_ConnectionStringPresent_ReturnsTrue()
   {
      var options = new TelemetryOptions("InstrumentationKey=00000000-0000-0000-0000-000000000000", "Production");
      await Assert.That(options.IsEnabled).IsTrue();
   }

   [Test]
   public async Task ConnectionStringKey_MatchesAppSettingsPath()
   {
      await Assert.That(TelemetryOptions.ConnectionStringKey).IsEqualTo("ApplicationInsights:ConnectionString");
   }
}
