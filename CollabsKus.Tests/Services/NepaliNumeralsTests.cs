using CollabsKus.BlazorWebAssembly.Services;

namespace CollabsKus.Tests.Services;

public class NepaliNumeralsTests
{
   [Test]
   public async Task ToNepaliDigits_Zero_Returns_DoubleZero()
   {
      await Assert.That(NepaliNumerals.ToNepaliDigits(0)).IsEqualTo("००");
   }

   [Test]
   public async Task ToNepaliDigits_SingleDigit_ReturnsPaddedNepali()
   {
      await Assert.That(NepaliNumerals.ToNepaliDigits(5)).IsEqualTo("०५");
   }

   [Test]
   public async Task ToNepaliDigits_TwoDigitNumber_ReturnsCorrectNepali()
   {
      await Assert.That(NepaliNumerals.ToNepaliDigits(12)).IsEqualTo("१२");
   }

   [Test]
   public async Task ToNepaliDigits_AllDigits_MapCorrectly()
   {
      string[] expected = ["००", "०१", "०२", "०३", "०४", "०५", "०६", "०७", "०८", "०९"];
      for (int i = 0; i < 10; i++)
      {
         await Assert.That(NepaliNumerals.ToNepaliDigits(i)).IsEqualTo(expected[i]);
      }
   }

   [Test]
   public async Task ToNepaliDigits_59_Returns_Correct()
   {
      await Assert.That(NepaliNumerals.ToNepaliDigits(59)).IsEqualTo("५९");
   }

   [Test]
   public async Task ToNepaliDigits_10_Returns_Correct()
   {
      await Assert.That(NepaliNumerals.ToNepaliDigits(10)).IsEqualTo("१०");
   }
}
