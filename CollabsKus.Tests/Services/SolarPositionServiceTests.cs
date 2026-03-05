using CollabsKus.BlazorWebAssembly.Services;

namespace CollabsKus.Tests.Services;

public class SolarPositionServiceTests
{
    private readonly SolarPositionService _service = new();

    [Test]
    public async Task Kathmandu_MidDay_HasPositiveAltitude()
    {
        // Mid-day UTC in summer — Kathmandu is UTC+5:45, so noon NST ≈ 06:15 UTC
        var utc = new DateTime(2025, 6, 21, 6, 15, 0, DateTimeKind.Utc);
        var pos = _service.CalculateKathmandu(utc);
        await Assert.That(pos.Altitude).IsGreaterThan(50.0);
        await Assert.That(pos.IsAboveHorizon).IsTrue();
    }

    [Test]
    public async Task Kathmandu_MidNight_HasNegativeAltitude()
    {
        // Midnight NST ≈ 18:15 UTC
        var utc = new DateTime(2025, 6, 21, 18, 15, 0, DateTimeKind.Utc);
        var pos = _service.CalculateKathmandu(utc);
        await Assert.That(pos.Altitude).IsLessThan(0.0);
        await Assert.That(pos.IsAboveHorizon).IsFalse();
    }

    [Test]
    public async Task Azimuth_IsAlways_Between0And360()
    {
        var baseDate = new DateTime(2025, 3, 20, 0, 0, 0, DateTimeKind.Utc);
        for (int hour = 0; hour < 24; hour++)
        {
            var utc = baseDate.AddHours(hour);
            var pos = _service.CalculateKathmandu(utc);
            await Assert.That(pos.Azimuth).IsGreaterThanOrEqualTo(0.0);
            await Assert.That(pos.Azimuth).IsLessThan(360.0);
        }
    }

    [Test]
    public async Task DayLength_InKathmandu_IsBetween10And14Hours()
    {
        // Test a few dates across the year
        var dates = new[]
        {
            new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc),  // winter
            new DateTime(2025, 3, 21, 12, 0, 0, DateTimeKind.Utc),  // equinox
            new DateTime(2025, 6, 21, 12, 0, 0, DateTimeKind.Utc),  // summer solstice
            new DateTime(2025, 9, 23, 12, 0, 0, DateTimeKind.Utc),  // equinox
            new DateTime(2025, 12, 21, 12, 0, 0, DateTimeKind.Utc), // winter solstice
        };

        foreach (var utc in dates)
        {
            var pos = _service.CalculateKathmandu(utc);
            await Assert.That(pos.DayLength.TotalHours).IsGreaterThan(10.0);
            await Assert.That(pos.DayLength.TotalHours).IsLessThan(14.0);
        }
    }

    [Test]
    public async Task Sunrise_IsBeforeSunset()
    {
        var utc = new DateTime(2025, 4, 10, 12, 0, 0, DateTimeKind.Utc);
        var pos = _service.CalculateKathmandu(utc);
        await Assert.That(pos.SunriseLocal.HasValue).IsTrue();
        await Assert.That(pos.SunsetLocal.HasValue).IsTrue();
        await Assert.That(pos.SunriseLocal!.Value < pos.SunsetLocal!.Value).IsTrue();
    }

    [Test]
    public async Task SolarNoon_IsBetweenSunriseAndSunset()
    {
        var utc = new DateTime(2025, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var pos = _service.CalculateKathmandu(utc);
        await Assert.That(pos.SunriseLocal.HasValue).IsTrue();
        await Assert.That(pos.SunsetLocal.HasValue).IsTrue();
        await Assert.That(pos.SolarNoonLocal > pos.SunriseLocal!.Value).IsTrue();
        await Assert.That(pos.SolarNoonLocal < pos.SunsetLocal!.Value).IsTrue();
    }

    [Test]
    public async Task MaxElevation_IsReasonableForKathmandu()
    {
        var summerUtc = new DateTime(2025, 6, 21, 12, 0, 0, DateTimeKind.Utc);
        var winterUtc = new DateTime(2025, 12, 21, 12, 0, 0, DateTimeKind.Utc);

        var summer = _service.CalculateKathmandu(summerUtc);
        var winter = _service.CalculateKathmandu(winterUtc);

        await Assert.That(summer.MaxElevation).IsGreaterThan(60.0);
        await Assert.That(winter.MaxElevation).IsGreaterThan(35.0);
        await Assert.That(winter.MaxElevation).IsLessThan(65.0);
    }

    [Test]
    public async Task DayFraction_IsZeroToOne_DuringDaytime()
    {
        // 9 AM NST ≈ 03:15 UTC
        var utc = new DateTime(2025, 5, 15, 3, 15, 0, DateTimeKind.Utc);
        var pos = _service.CalculateKathmandu(utc);
        if (pos.IsAboveHorizon)
        {
            await Assert.That(pos.DayFraction).IsGreaterThanOrEqualTo(0.0);
            await Assert.That(pos.DayFraction).IsLessThanOrEqualTo(1.0);
        }
    }

    [Test]
    public async Task Calculate_WithCustomLocation_Works()
    {
        // New York: 40.7128°N, 74.0060°W, UTC-5
        var utc = new DateTime(2025, 6, 15, 17, 0, 0, DateTimeKind.Utc); // noon EDT
        var tzOffset = TimeSpan.FromHours(-4); // EDT
        var pos = _service.Calculate(utc, 40.7128, -74.0060, tzOffset, "New York");

        await Assert.That(pos.LocationName).IsEqualTo("New York");
        await Assert.That(pos.Latitude).IsEqualTo(40.7128);
        await Assert.That(pos.Altitude).IsGreaterThan(40.0); // midday altitude
    }

    [Test]
    public async Task PreviousSunrise_IsBeforeNow()
    {
        // 3 PM NST ≈ 09:15 UTC (well after sunrise)
        var utc = new DateTime(2025, 4, 10, 9, 15, 0, DateTimeKind.Utc);
        var pos = _service.CalculateKathmandu(utc);
        var localNow = utc + SolarPositionService.NstOffset;

        await Assert.That(pos.PreviousSunrise.HasValue).IsTrue();
        await Assert.That(pos.PreviousSunrise!.Value <= localNow).IsTrue();
    }

    [Test]
    public async Task NextSunset_IsAfterNow()
    {
        // 10 AM NST ≈ 04:15 UTC (before sunset)
        var utc = new DateTime(2025, 4, 10, 4, 15, 0, DateTimeKind.Utc);
        var pos = _service.CalculateKathmandu(utc);
        var localNow = utc + SolarPositionService.NstOffset;

        await Assert.That(pos.NextSunset.HasValue).IsTrue();
        await Assert.That(pos.NextSunset!.Value > localNow).IsTrue();
    }

    [Test]
    public async Task LocationName_IsSetCorrectly()
    {
        var utc = new DateTime(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        var pos = _service.CalculateKathmandu(utc);
        await Assert.That(pos.LocationName).IsEqualTo("Kathmandu, Nepal");
    }

    [Test]
    public async Task AzimuthCardinal_ReturnsValidDirection()
    {
        var validDirs = new HashSet<string>
        {
            "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE",
            "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"
        };

        var utc = new DateTime(2025, 5, 1, 8, 0, 0, DateTimeKind.Utc);
        var pos = _service.CalculateKathmandu(utc);
        await Assert.That(validDirs.Contains(pos.AzimuthCardinal)).IsTrue();
    }

    // ── Regression tests: sunrise/sunset reasonableness ───────────────────

    [Test]
    public async Task Kathmandu_SunriseIsBetween4AMAnd7AM_AllYear()
    {
        var fourAm = new TimeOnly(4, 0);
        var sevenAm = new TimeOnly(7, 0);

        for (int month = 1; month <= 12; month++)
        {
            var utc = new DateTime(2025, month, 15, 12, 0, 0, DateTimeKind.Utc);
            var pos = _service.CalculateKathmandu(utc);

            await Assert.That(pos.SunriseLocal.HasValue).IsTrue();
            await Assert.That(pos.SunriseLocal!.Value >= fourAm).IsTrue();
            await Assert.That(pos.SunriseLocal!.Value <= sevenAm).IsTrue();
        }
    }

    [Test]
    public async Task Kathmandu_SunsetIsBetween5PMAnd710PM_AllYear()
    {
        // Kathmandu sunset ranges ~5:20 (Dec) to ~19:03 (June solstice).
        // Allow 17:00–19:10 for safety margin on the Meeus approximation.
        var fivePm = new TimeOnly(17, 0);
        var lateSunset = new TimeOnly(19, 10);

        for (int month = 1; month <= 12; month++)
        {
            var utc = new DateTime(2025, month, 15, 12, 0, 0, DateTimeKind.Utc);
            var pos = _service.CalculateKathmandu(utc);

            await Assert.That(pos.SunsetLocal.HasValue).IsTrue();
            await Assert.That(pos.SunsetLocal!.Value >= fivePm).IsTrue();
            await Assert.That(pos.SunsetLocal!.Value <= lateSunset).IsTrue();
        }
    }

    [Test]
    public async Task Kathmandu_SolarNoonIsBetween1145AMAnd1220PM_AllYear()
    {
        var earlyNoon = new TimeOnly(11, 45);
        var lateNoon = new TimeOnly(12, 20);

        for (int month = 1; month <= 12; month++)
        {
            var utc = new DateTime(2025, month, 15, 12, 0, 0, DateTimeKind.Utc);
            var pos = _service.CalculateKathmandu(utc);

            await Assert.That(pos.SolarNoonLocal >= earlyNoon).IsTrue();
            await Assert.That(pos.SolarNoonLocal <= lateNoon).IsTrue();
        }
    }

    [Test]
    public async Task WesternHemisphere_SunriseAndSunsetAreReasonable()
    {
        var utc = new DateTime(2025, 3, 20, 12, 0, 0, DateTimeKind.Utc);
        var tzOffset = TimeSpan.FromHours(-4); // EDT
        var pos = _service.Calculate(utc, 40.7128, -74.0060, tzOffset, "New York");

        await Assert.That(pos.SunriseLocal.HasValue).IsTrue();
        await Assert.That(pos.SunsetLocal.HasValue).IsTrue();
        await Assert.That(pos.SunriseLocal!.Value < pos.SunsetLocal!.Value).IsTrue();

        await Assert.That(pos.SunriseLocal!.Value >= new TimeOnly(6, 0)).IsTrue();
        await Assert.That(pos.SunriseLocal!.Value <= new TimeOnly(8, 0)).IsTrue();

        await Assert.That(pos.SunsetLocal!.Value >= new TimeOnly(18, 0)).IsTrue();
        await Assert.That(pos.SunsetLocal!.Value <= new TimeOnly(20, 0)).IsTrue();
    }

    // ── FullDayFraction tests ─────────────────────────────────────────────

    [Test]
    public async Task FullDayFraction_IsAlways_Between0And1()
    {
        var baseDate = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        for (int hour = 0; hour < 24; hour++)
        {
            var utc = baseDate.AddHours(hour);
            var pos = _service.CalculateKathmandu(utc);
            await Assert.That(pos.FullDayFraction).IsGreaterThanOrEqualTo(0.0);
            await Assert.That(pos.FullDayFraction).IsLessThan(1.0);
        }
    }

    [Test]
    public async Task FullDayFraction_NearNoon_IsAround05()
    {
        // Noon NST ≈ 06:15 UTC
        var utc = new DateTime(2025, 6, 15, 6, 15, 0, DateTimeKind.Utc);
        var pos = _service.CalculateKathmandu(utc);

        // Should be close to 0.5 (solar noon)
        await Assert.That(pos.FullDayFraction).IsGreaterThan(0.45);
        await Assert.That(pos.FullDayFraction).IsLessThan(0.55);
    }

    [Test]
    public async Task FullDayFraction_NearMidnight_IsNear0Or1()
    {
        // Midnight NST ≈ 18:15 UTC
        var utc = new DateTime(2025, 6, 15, 18, 15, 0, DateTimeKind.Utc);
        var pos = _service.CalculateKathmandu(utc);

        // Should be close to 0 or 1 (solar midnight)
        var nearMidnight = pos.FullDayFraction < 0.05 || pos.FullDayFraction > 0.95;
        await Assert.That(nearMidnight).IsTrue();
    }

    [Test]
    public async Task FullDayFraction_IncreasesOverDay()
    {
        // Test that FullDayFraction increases monotonically from morning to evening
        var morning = new DateTime(2025, 4, 10, 2, 0, 0, DateTimeKind.Utc);   // ~7:45 NST
        var midday = new DateTime(2025, 4, 10, 6, 15, 0, DateTimeKind.Utc);   // ~12:00 NST
        var evening = new DateTime(2025, 4, 10, 12, 0, 0, DateTimeKind.Utc);  // ~17:45 NST

        var posMorning = _service.CalculateKathmandu(morning);
        var posMidday = _service.CalculateKathmandu(midday);
        var posEvening = _service.CalculateKathmandu(evening);

        await Assert.That(posMidday.FullDayFraction).IsGreaterThan(posMorning.FullDayFraction);
        await Assert.That(posEvening.FullDayFraction).IsGreaterThan(posMidday.FullDayFraction);
    }
}
