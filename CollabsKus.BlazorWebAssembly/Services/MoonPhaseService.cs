using CollabsKus.BlazorWebAssembly.Models;

namespace CollabsKus.BlazorWebAssembly.Services;

public class MoonPhaseService
{
    private readonly List<MoonPhaseDefinition> _phaseDefinitions = new()
    {
        new("New Moon",              "🌑", 0.0,      1.0),
        new("Waxing Crescent",       "🌒", 1.0,      7.38),
        new("First Quarter",         "🌓", 7.38,     8.38),
        new("Waxing Gibbous",        "🌔", 8.38,     14.77),
        new("Full Moon",             "🌕", 14.77,    15.77),
        new("Waning Gibbous",        "🌖", 15.77,    22.15),
        new("Last Quarter",          "🌗", 22.15,    23.15),
        new("Waning Crescent",       "🌘", 23.15,    28.53),
    };

    public MoonPhase CalculateMoonPhase(DateTime date)
    {
        var utcDate = date.Kind == DateTimeKind.Utc ? date : date.ToUniversalTime();

        var year = utcDate.Year;
        var month = utcDate.Month;
        var day = utcDate.Day;
        var hour = utcDate.Hour;
        var minute = utcDate.Minute;
        var second = utcDate.Second;

        var fractionalDay = day + (hour / 24.0) + (minute / 1440.0) + (second / 86400.0);

        var a = (14 - month) / 12;          // integer floor — intentional
        var y = year + 4800 - a;
        var m = month + 12 * a - 3;

        // Fix: use 5.0 to avoid integer division truncation
        var jdn = fractionalDay + (153 * m + 2) / 5.0 + 365.0 * y +
                  y / 4 - y / 100 + y / 400 - 32045;

        const double knownNewMoon = 2451550.1;
        const double synodicMonth = 29.53058867;

        var daysSinceNew = jdn - knownNewMoon;
        var newMoons = daysSinceNew / synodicMonth;
        var moonAge = (newMoons - Math.Floor(newMoons)) * synodicMonth;

        if (moonAge < 0) moonAge += synodicMonth;
        else if (moonAge >= synodicMonth) moonAge -= synodicMonth;

        var moonPhaseAngle = (moonAge / synodicMonth) * 2 * Math.PI;
        var illumination = Math.Max(0, Math.Min(1, (1 - Math.Cos(moonPhaseAngle)) / 2));

        var phase = GetMoonPhase(moonAge, synodicMonth);

        return new MoonPhase
        {
            Name = phase.Name,
            Icon = phase.Icon,
            Illumination = Math.Round(illumination * 100, 2),
            Age = moonAge
        };
    }

    private MoonPhaseDefinition GetMoonPhase(double age, double synodicMonth)
    {
        if (age >= 28.53)
            return _phaseDefinitions[0]; // New Moon (end of cycle wraps back)

        foreach (var phase in _phaseDefinitions)
            if (age >= phase.Min && age < phase.Max)
                return phase;

        return _phaseDefinitions[0];
    }

    private record MoonPhaseDefinition(string Name, string Icon, double Min, double Max);
}
