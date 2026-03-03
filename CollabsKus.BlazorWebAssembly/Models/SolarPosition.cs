namespace CollabsKus.BlazorWebAssembly.Models;

/// <summary>
/// Instantaneous sun position and daily solar events for Kathmandu.
/// All calculations are pure astronomy — no network requests.
/// </summary>
public class SolarPosition
{
    // ── Instantaneous values (change every second) ────────────────────────
    /// <summary>Elevation angle above the horizon in degrees. Negative = below horizon.</summary>
    public double Altitude { get; init; }

    /// <summary>Compass bearing in degrees (0 = North, 90 = East, 180 = South, 270 = West).</summary>
    public double Azimuth { get; init; }

    /// <summary>Solar declination in degrees (positive = northern hemisphere tilt).</summary>
    public double Declination { get; init; }

    /// <summary>Local hour angle in degrees (negative = morning, 0 = noon, positive = afternoon).</summary>
    public double HourAngle { get; init; }

    /// <summary>True if the sun is above the horizon.</summary>
    public bool IsAboveHorizon => Altitude > 0;

    /// <summary>True if within golden hour window (sun between -6° and +6°).</summary>
    public bool IsGoldenHour => Altitude is > -6 and < 6;

    /// <summary>Fraction of the day arc traveled (0 = sunrise, 1 = sunset). -1 if polar night/day.</summary>
    public double DayFraction { get; init; }

    // ── Daily events (recomputed once per Nepal calendar day) ─────────────
    /// <summary>Sunrise time in Nepal Standard Time. Null during polar phenomena.</summary>
    public TimeOnly? SunriseNST { get; init; }

    /// <summary>Solar noon time in Nepal Standard Time.</summary>
    public TimeOnly SolarNoonNST { get; init; }

    /// <summary>Sunset time in Nepal Standard Time. Null during polar phenomena.</summary>
    public TimeOnly? SunsetNST { get; init; }

    /// <summary>Start of morning golden hour (sun at -6°) in NST.</summary>
    public TimeOnly? GoldenHourMorningStart { get; init; }

    /// <summary>End of morning golden hour (sun at +6°) in NST.</summary>
    public TimeOnly? GoldenHourMorningEnd { get; init; }

    /// <summary>Start of evening golden hour (sun at +6°) in NST.</summary>
    public TimeOnly? GoldenHourEveningStart { get; init; }

    /// <summary>End of evening golden hour (sun at -6°) in NST.</summary>
    public TimeOnly? GoldenHourEveningEnd { get; init; }

    /// <summary>Maximum sun elevation at solar noon, degrees.</summary>
    public double MaxElevation { get; init; }

    /// <summary>Total daylight duration. Zero during polar night.</summary>
    public TimeSpan DayLength { get; init; }

    // ── Helpers ───────────────────────────────────────────────────────────
    public string AzimuthCardinal => AzToCardinal(Azimuth);
    public string AltitudeSign => Altitude >= 0 ? "+" : "";

    private static string AzToCardinal(double az)
    {
        string[] dirs = ["N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"];
        return dirs[(int)Math.Round(az / 22.5) % 16];
    }
}
