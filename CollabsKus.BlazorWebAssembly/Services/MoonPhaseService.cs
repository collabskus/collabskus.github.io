using CollabsKus.BlazorWebAssembly.Models;

namespace CollabsKus.BlazorWebAssembly.Services;

public class MoonPhaseService
{
    private const double SynodicMonth = 29.53058867;

    // Known new moon: January 6, 2000 at 18:14 UTC
    private const double KnownNewMoon = 2451550.1;

    // Full moon is at age = SynodicMonth / 2
    private const double FullMoonAge = SynodicMonth / 2.0; // ~14.765 days

    private readonly List<MoonPhaseDefinition> _phaseDefinitions = new()
    {
        new("New Moon",        "🌑", 0.0,   1.0),
        new("Waxing Crescent", "🌒", 1.0,   7.38),
        new("First Quarter",   "🌓", 7.38,  8.38),
        new("Waxing Gibbous",  "🌔", 8.38,  14.77),
        new("Full Moon",       "🌕", 14.77, 15.77),
        new("Waning Gibbous",  "🌖", 15.77, 22.15),
        new("Last Quarter",    "🌗", 22.15, 23.15),
        new("Waning Crescent", "🌘", 23.15, 28.53),
    };

    // Traditional tithi names in Sanskrit/Nepali, 1–15 per paksha
    private static readonly string[] TithiNames =
    {
        "Pratipada", // 1
        "Dwitiya",   // 2
        "Tritiya",   // 3
        "Chaturthi", // 4
        "Panchami",  // 5
        "Shashthi",  // 6
        "Saptami",   // 7
        "Ashtami",   // 8
        "Navami",    // 9
        "Dashami",   // 10
        "Ekadashi",  // 11
        "Dwadashi",  // 12
        "Trayodashi",// 13
        "Chaturdashi",// 14
        "Purnima / Amavasya" // 15 — full moon in Shukla, new moon in Krishna
    };

    public MoonPhase CalculateMoonPhase(DateTime date)
    {
        var moonAge = CalculateMoonAge(date);

        var moonPhaseAngle = (moonAge / SynodicMonth) * 2 * Math.PI;
        var illumination = Math.Max(0, Math.Min(1, (1 - Math.Cos(moonPhaseAngle)) / 2));

        var phase = GetMoonPhase(moonAge);

        // Days since last new moon
        var daysSinceNewMoon = moonAge;

        // Days since last full moon
        double daysSinceFullMoon;
        if (moonAge >= FullMoonAge)
            daysSinceFullMoon = moonAge - FullMoonAge;
        else
            daysSinceFullMoon = moonAge + (SynodicMonth - FullMoonAge); // previous cycle's full moon

        // Days until next new moon
        var daysUntilNewMoon = SynodicMonth - moonAge;

        // Days until next full moon
        double daysUntilFullMoon;
        if (moonAge < FullMoonAge)
            daysUntilFullMoon = FullMoonAge - moonAge;
        else
            daysUntilFullMoon = SynodicMonth - moonAge + FullMoonAge;

        // Tithi calculation
        // Each tithi = SynodicMonth / 30 days (~0.9843 days)
        // Tithis 1–15 = Shukla Paksha (waxing), 16–30 = Krishna Paksha (waning)
        // We map to 0-based index of 30 tithis
        var tithiIndex = (int)(moonAge / (SynodicMonth / 30.0)); // 0–29
        tithiIndex = Math.Min(tithiIndex, 29); // clamp for floating-point edge cases

        string paksha;
        int tithiNumber;
        string tithiName;

        if (tithiIndex < 15)
        {
            // Shukla Paksha (bright/waxing fortnight): tithis 1–15
            paksha = "Shukla Paksha";
            tithiNumber = tithiIndex + 1;
            tithiName = tithiIndex == 14 ? "Purnima" : TithiNames[tithiIndex];
        }
        else
        {
            // Krishna Paksha (dark/waning fortnight): tithis 1–15
            paksha = "Krishna Paksha";
            tithiNumber = tithiIndex - 14; // 15→1, 16→2, ... 29→15
            tithiName = tithiIndex == 29 ? "Amavasya" : TithiNames[tithiIndex - 15];
        }

        return new MoonPhase
        {
            Name = phase.Name,
            Icon = phase.Icon,
            Illumination = Math.Round(illumination * 100, 2),
            Age = moonAge,
            DaysSinceNewMoon = Math.Round(daysSinceNewMoon, 1),
            DaysSinceFullMoon = Math.Round(daysSinceFullMoon, 1),
            DaysUntilNewMoon = Math.Round(daysUntilNewMoon, 1),
            DaysUntilFullMoon = Math.Round(daysUntilFullMoon, 1),
            TithiNumber = tithiNumber,
            TithiName = tithiName,
            Paksha = paksha
        };
    }

    private double CalculateMoonAge(DateTime date)
    {
        var utcDate = date.Kind == DateTimeKind.Utc ? date : date.ToUniversalTime();

        var year = utcDate.Year;
        var month = utcDate.Month;
        var day = utcDate.Day;
        var hour = utcDate.Hour;
        var minute = utcDate.Minute;
        var second = utcDate.Second;

        var fractionalDay = day + (hour / 24.0) + (minute / 1440.0) + (second / 86400.0);

        var a = (14 - month) / 12;     // integer floor — intentional
        var y = year + 4800 - a;
        var m = month + 12 * a - 3;

        // Use 5.0 to avoid integer division truncation
        var jdn = fractionalDay + (153 * m + 2) / 5.0 + 365.0 * y +
                  y / 4 - y / 100 + y / 400 - 32045;

        var daysSinceNew = jdn - KnownNewMoon;
        var newMoons = daysSinceNew / SynodicMonth;
        var moonAge = (newMoons - Math.Floor(newMoons)) * SynodicMonth;

        if (moonAge < 0) moonAge += SynodicMonth;
        else if (moonAge >= SynodicMonth) moonAge -= SynodicMonth;

        return moonAge;
    }

    private MoonPhaseDefinition GetMoonPhase(double age)
    {
        if (age >= 28.53)
            return _phaseDefinitions[0]; // end of cycle → New Moon

        foreach (var phase in _phaseDefinitions)
            if (age >= phase.Min && age < phase.Max)
                return phase;

        return _phaseDefinitions[0];
    }

    private record MoonPhaseDefinition(string Name, string Icon, double Min, double Max);
}
