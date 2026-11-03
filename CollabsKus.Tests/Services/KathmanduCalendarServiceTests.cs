using CollabsKus.BlazorWebAssembly.Services;

namespace CollabsKus.Tests.Services;

public class KathmanduCalendarServiceTests
{
   [Test]
   public async Task ToNepaliDigits_Zero_Returns_DoubleZero()
   {
      string result = KathmanduCalendarService.ToNepaliDigits(0);
      await Assert.That(result).IsEqualTo("००");
   }

   [Test]
   public async Task ToNepaliDigits_SingleDigit_ReturnsPaddedNepali()
   {
      string result = KathmanduCalendarService.ToNepaliDigits(5);
      await Assert.That(result).IsEqualTo("०५");
   }

   [Test]
   public async Task ToNepaliDigits_TwoDigitNumber_ReturnsCorrectNepali()
   {
      string result = KathmanduCalendarService.ToNepaliDigits(12);
      await Assert.That(result).IsEqualTo("१२");
   }

   [Test]
   public async Task ToNepaliDigits_AllDigits_MapCorrectly()
   {
      // Test each digit 0-9
      string[] expected = new[] { "००", "०१", "०२", "०३", "०४", "०५", "०६", "०७", "०८", "०९" };
      for (int i = 0; i < 10; i++)
      {
         string result = KathmanduCalendarService.ToNepaliDigits(i);
         await Assert.That(result).IsEqualTo(expected[i]);
      }
   }

   [Test]
   public async Task ToNepaliDigits_59_Returns_Correct()
   {
      string result = KathmanduCalendarService.ToNepaliDigits(59);
      await Assert.That(result).IsEqualTo("५९");
   }

   [Test]
   public async Task ToNepaliDigits_10_Returns_Correct()
   {
      string result = KathmanduCalendarService.ToNepaliDigits(10);
      await Assert.That(result).IsEqualTo("१०");
   }
}
