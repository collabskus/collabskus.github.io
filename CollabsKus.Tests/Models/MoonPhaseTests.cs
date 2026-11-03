using CollabsKus.BlazorWebAssembly.Models;
using CollabsKus.BlazorWebAssembly.Services;

namespace CollabsKus.Tests.Models;

public class MoonPhaseTests
{
   private const double SynodicMonth = 29.53058770576;

   [Test]
   public async Task IsWaxing_MatchesFirstHalfOfCycle()
   {
      var baseDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
      for (int hours = 0; hours < 24 * 30; hours += 7)
      {
         var phase = MoonPhaseService.CalculateMoonPhase(baseDate.AddHours(hours));
         await Assert.That(phase.IsWaxing).IsEqualTo(phase.Age < SynodicMonth / 2);
      }
   }

   [Test]
   public async Task IsWaxing_TrueWhenFullMoonIsNearer()
   {
      var phase = new MoonPhase { DaysUntilFullMoon = 3, DaysUntilNewMoon = 18 };
      await Assert.That(phase.IsWaxing).IsTrue();
   }

   [Test]
   public async Task IsWaxing_FalseWhenNewMoonIsNearer()
   {
      var phase = new MoonPhase { DaysUntilFullMoon = 25, DaysUntilNewMoon = 10 };
      await Assert.That(phase.IsWaxing).IsFalse();
   }
}
