using CollabsKus.BlazorWebAssembly.Models;
using CollabsKus.BlazorWebAssembly.Services;

namespace CollabsKus.Tests.Services;

public class ClockOffsetCalculatorTests
{
   private static readonly TimeSpan Nepal = TimeSpan.FromMinutes(345);

   [Test]
   public async Task Calculate_SameDayAfternoon_ReturnsNepalOffset()
   {
      var utc = new DateTime(2026, 4, 14, 10, 0, 0, DateTimeKind.Utc);
      var offset = ClockOffsetCalculator.Calculate(Response("3", "45", "00", "PM"), utc, utc, Nepal);
      await Assert.That(offset!.Value).IsEqualTo(Nepal);
   }

   [Test]
   public async Task Calculate_AcrossUtcMidnight_CorrectsDateBoundary()
   {
      var utc = new DateTime(2026, 4, 14, 23, 0, 0, DateTimeKind.Utc);
      var offset = ClockOffsetCalculator.Calculate(Response("4", "45", "00", "AM"), utc, utc, Nepal);
      await Assert.That(offset!.Value).IsEqualTo(Nepal);
   }

   [Test]
   public async Task Calculate_KathmanduMidnight_HandlesTwelveAm()
   {
      var utc = new DateTime(2026, 4, 14, 18, 15, 0, DateTimeKind.Utc);
      var offset = ClockOffsetCalculator.Calculate(Response("12", "00", "00", "AM"), utc, utc, Nepal);
      await Assert.That(offset!.Value).IsEqualTo(Nepal);
   }

   [Test]
   public async Task Calculate_KathmanduNoon_HandlesTwelvePm()
   {
      var utc = new DateTime(2026, 4, 14, 6, 15, 0, DateTimeKind.Utc);
      var offset = ClockOffsetCalculator.Calculate(Response("12", "00", "00", "PM"), utc, utc, Nepal);
      await Assert.That(offset!.Value).IsEqualTo(Nepal);
   }

   [Test]
   public async Task Calculate_UsesRequestMidpoint()
   {
      var start = new DateTime(2026, 4, 14, 10, 0, 0, DateTimeKind.Utc);
      var end = start.AddSeconds(2);
      var offset = ClockOffsetCalculator.Calculate(Response("3", "45", "01", "PM"), start, end, Nepal);
      await Assert.That(offset!.Value).IsEqualTo(Nepal);
   }

   [Test]
   public async Task Calculate_PreservesDeviceClockSkew()
   {
      var utc = new DateTime(2026, 4, 14, 10, 0, 0, DateTimeKind.Utc);
      var offset = ClockOffsetCalculator.Calculate(Response("3", "45", "30", "PM"), utc, utc, Nepal);
      await Assert.That(offset!.Value).IsEqualTo(Nepal + TimeSpan.FromSeconds(30));
   }

   [Test]
   public async Task Calculate_NegativeExpectedOffset_NormalizesAroundExpected()
   {
      var utc = new DateTime(2026, 4, 14, 2, 0, 0, DateTimeKind.Utc);
      var expected = TimeSpan.FromHours(-5);
      var offset = ClockOffsetCalculator.Calculate(Response("9", "00", "00", "PM"), utc, utc, expected);
      await Assert.That(offset!.Value).IsEqualTo(expected);
   }

   [Test]
   [Arguments("x", "00", "00")]
   [Arguments("3", "61", "00")]
   [Arguments("3", "00", "")]
   public async Task Calculate_InvalidComponents_ReturnsNull(string hour, string minute, string second)
   {
      var utc = new DateTime(2026, 4, 14, 10, 0, 0, DateTimeKind.Utc);
      var offset = ClockOffsetCalculator.Calculate(Response(hour, minute, second, "PM"), utc, utc, Nepal);
      await Assert.That(offset.HasValue).IsFalse();
   }

   [Test]
   public async Task Normalize_WithinHalfDay_IsUnchanged()
   {
      var value = TimeSpan.FromHours(7);
      await Assert.That(ClockOffsetCalculator.Normalize(value, Nepal)).IsEqualTo(value);
   }

   private static TimeResponse Response(string hour, string minute, string second, string amPm) => new()
   {
      Hour = hour,
      Min = minute,
      Sec = second,
      ApOrPm = amPm
   };
}
