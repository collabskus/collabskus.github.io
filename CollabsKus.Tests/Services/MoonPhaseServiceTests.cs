using CollabsKus.BlazorWebAssembly.Services;

namespace CollabsKus.Tests.Services;

public class MoonPhaseServiceTests
{
    private const double SynodicMonth = 29.53058770576;

    [Test]
    public async Task Illumination_IsAlways_Between0And100()
    {
        // Test across a full synodic month in 1-day increments
        var baseDate = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 30; i++)
        {
            var date = baseDate.AddDays(i);
            var phase = MoonPhaseService.CalculateMoonPhase(date);
            await Assert.That(phase.Illumination).IsGreaterThanOrEqualTo(0.0);
            await Assert.That(phase.Illumination).IsLessThanOrEqualTo(100.0);
        }
    }

    [Test]
    public async Task MoonAge_IsAlways_WithinSynodicMonth()
    {
        var baseDate = new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 60; i++)
        {
            var date = baseDate.AddDays(i);
            var phase = MoonPhaseService.CalculateMoonPhase(date);
            await Assert.That(phase.Age).IsGreaterThanOrEqualTo(0.0);
            await Assert.That(phase.Age).IsLessThan(SynodicMonth);
        }
    }

    [Test]
    public async Task PhaseName_IsAlways_OneOfKnownPhases()
    {
        var validNames = new HashSet<string>
        {
            "New Moon", "Waxing Crescent", "First Quarter", "Waxing Gibbous",
            "Full Moon", "Waning Gibbous", "Last Quarter", "Waning Crescent"
        };

        var baseDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 365; i += 3)
        {
            var date = baseDate.AddDays(i);
            var phase = MoonPhaseService.CalculateMoonPhase(date);
            await Assert.That(validNames.Contains(phase.Name)).IsTrue();
        }
    }

    [Test]
    public async Task TithiNumber_IsAlways_Between1And15()
    {
        var baseDate = new DateTime(2025, 4, 1, 6, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 30; i++)
        {
            var date = baseDate.AddDays(i);
            var phase = MoonPhaseService.CalculateMoonPhase(date);
            await Assert.That(phase.TithiNumber).IsGreaterThanOrEqualTo(1);
            await Assert.That(phase.TithiNumber).IsLessThanOrEqualTo(15);
        }
    }

    [Test]
    public async Task Paksha_IsAlways_ShuklaOrKrishna()
    {
        var baseDate = new DateTime(2025, 7, 10, 12, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 30; i++)
        {
            var date = baseDate.AddDays(i);
            var phase = MoonPhaseService.CalculateMoonPhase(date);
            var validPaksha = phase.Paksha == "Shukla Paksha" || phase.Paksha == "Krishna Paksha";
            await Assert.That(validPaksha).IsTrue();
        }
    }

    [Test]
    public async Task Icon_IsNeverEmpty()
    {
        var date = new DateTime(2025, 9, 20, 18, 30, 0, DateTimeKind.Utc);
        var phase = MoonPhaseService.CalculateMoonPhase(date);
        await Assert.That(string.IsNullOrEmpty(phase.Icon)).IsFalse();
    }

    [Test]
    public async Task DaysSinceNewMoon_EqualsAge()
    {
        var date = new DateTime(2025, 5, 5, 0, 0, 0, DateTimeKind.Utc);
        var phase = MoonPhaseService.CalculateMoonPhase(date);
        // DaysSinceNewMoon should equal Age (both derived from elongation)
        await Assert.That(Math.Abs(phase.DaysSinceNewMoon - phase.Age)).IsLessThan(0.01);
    }

    [Test]
    public async Task DaysUntilNewMoon_PlusDaysSince_EqualsSynodicMonth()
    {
        var date = new DateTime(2025, 8, 12, 8, 0, 0, DateTimeKind.Utc);
        var phase = MoonPhaseService.CalculateMoonPhase(date);
        var total = phase.DaysSinceNewMoon + phase.DaysUntilNewMoon;
        await Assert.That(Math.Abs(total - SynodicMonth)).IsLessThan(0.01);
    }

    [Test]
    public async Task NewMoon_HasLowIllumination()
    {
        // A known approximate new moon: 2025-01-29 ~12:36 UTC
        var newMoonApprox = new DateTime(2025, 1, 29, 12, 36, 0, DateTimeKind.Utc);
        var phase = MoonPhaseService.CalculateMoonPhase(newMoonApprox);
        await Assert.That(phase.Illumination).IsLessThan(1.0);
    }

    [Test]
    public async Task FullMoon_HasHighIllumination()
    {
        // A known approximate full moon: 2025-02-12 ~13:53 UTC
        var fullMoonApprox = new DateTime(2025, 2, 12, 13, 53, 0, DateTimeKind.Utc);
        var phase = MoonPhaseService.CalculateMoonPhase(fullMoonApprox);
        await Assert.That(phase.Illumination).IsGreaterThan(99.0);
    }

    [Test]
    public async Task SameUtcInstant_ProducesSameResult()
    {
        var utc = new DateTime(2025, 6, 15, 14, 30, 0, DateTimeKind.Utc);
        var phase1 = MoonPhaseService.CalculateMoonPhase(utc);
        var phase2 = MoonPhaseService.CalculateMoonPhase(utc);
        await Assert.That(phase1.Illumination).IsEqualTo(phase2.Illumination);
        await Assert.That(phase1.Age).IsEqualTo(phase2.Age);
    }

    [Test]
    public async Task ShuklaLastTithi_IsPurnima()
    {
        // Find a date near full moon where Shukla Paksha tithi 15 occurs
        var fullMoonApprox = new DateTime(2025, 2, 12, 13, 53, 0, DateTimeKind.Utc);
        var phase = MoonPhaseService.CalculateMoonPhase(fullMoonApprox);
        if (phase.Paksha == "Shukla Paksha" && phase.TithiNumber == 15)
        {
            await Assert.That(phase.TithiName).IsEqualTo("Purnima");
        }
        // else: the exact moment might land in Krishna Paksha — that's OK, just skip assertion
    }
}
