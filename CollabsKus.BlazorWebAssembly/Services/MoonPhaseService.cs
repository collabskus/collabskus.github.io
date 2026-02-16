using CollabsKus.BlazorWebAssembly.Models;

namespace CollabsKus.BlazorWebAssembly.Services;

public class MoonPhaseService
{
    private readonly List<MoonPhaseDefinition> _phaseDefinitions = new()
    {
        new("New Moon", "🌑", 0, 1.84566),
        new("Waxing Crescent", "🌒", 1.84566, 5.53699),
        new("First Quarter", "🌓", 5.53699, 9.22831),
        new("Waxing Gibbous", "🌔", 9.22831, 12.91963),
        new("Full Moon", "🌕", 12.91963, 16.61096),
        new("Waning Gibbous", "🌖", 16.61096, 20.30228),
        new("Last Quarter", "🌗", 20.30228, 23.99361),
        new("Waning Crescent", "🌘", 23.99361, 27.69134)
    };

    public MoonPhase CalculateMoonPhase(DateTime date)
    {
        // Convert DateTime to UTC if it's not already
        var utcDate = date.Kind == DateTimeKind.Utc ? date : date.ToUniversalTime();

        // Convert to Julian Day Number WITH time component
        var year = utcDate.Year;
        var month = utcDate.Month;
        var day = utcDate.Day;

        // Calculate fractional day (time of day as fraction)
        var hour = utcDate.Hour;
        var minute = utcDate.Minute;
        var second = utcDate.Second;
        var fractionalDay = day + (hour / 24.0) + (minute / 1440.0) + (second / 86400.0);

        var a = (14 - month) / 12;
        var y = year + 4800 - a;
        var m = month + 12 * a - 3;

        var jdn = fractionalDay + (153 * m + 2) / 5 + 365 * y +
                  y / 4 - y / 100 + y / 400 - 32045;

        // Known new moon: January 6, 2000 at 18:14 UTC
        const double knownNewMoon = 2451550.1;
        var daysSinceNew = jdn - knownNewMoon;

        // Synodic month (average lunar cycle length)
        const double synodicMonth = 29.53058867;

        // Calculate moon age (days into current lunar cycle)
        var newMoons = daysSinceNew / synodicMonth;
        var moonAge = (newMoons - Math.Floor(newMoons)) * synodicMonth;

        // Ensure moonAge is in valid range [0, synodicMonth)
        if (moonAge < 0)
        {
            moonAge += synodicMonth;
        }
        else if (moonAge >= synodicMonth)
        {
            moonAge -= synodicMonth;
        }

        // Calculate illumination
        var moonPhaseAngle = (moonAge / synodicMonth) * 2 * Math.PI;
        var illumination = (1 - Math.Cos(moonPhaseAngle)) / 2;

        // Clamp illumination to [0, 1] to handle floating-point precision issues
        illumination = Math.Max(0, Math.Min(1, illumination));

        var phase = GetMoonPhaseName(moonAge);

        return new MoonPhase
        {
            Name = phase.Name,
            Icon = phase.Icon,
            Illumination = Math.Round(illumination * 100, 2), // Round to 2 decimal places
            Age = moonAge
        };
    }

    private MoonPhaseDefinition GetMoonPhaseName(double age)
    {
        // New Moon spans the end and beginning of the cycle
        // Age >= 27.69134 days means we're in the New Moon phase (end of cycle)
        if (age >= 27.69134)
        {
            return _phaseDefinitions[0]; // New Moon
        }

        // Check all other phases
        foreach (var phase in _phaseDefinitions)
        {
            if (age >= phase.Min && age < phase.Max)
            {
                return phase;
            }
        }

        // Fallback to New Moon (this handles age 0 to 1.84566)
        return _phaseDefinitions[0];
    }

    private record MoonPhaseDefinition(string Name, string Icon, double Min, double Max);
}
