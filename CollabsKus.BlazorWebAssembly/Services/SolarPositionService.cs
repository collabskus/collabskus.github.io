using CollabsKus.BlazorWebAssembly.Models;

namespace CollabsKus.BlazorWebAssembly.Services;

/// <summary>
/// Calculates sun position and daily solar events for Kathmandu using the
/// Jean Meeus astronomical algorithm (Astronomical Algorithms, 2nd ed.).
/// Pure math — no network calls, no JS interop.
/// </summary>
public class SolarPositionService
{
    // Kathmandu coordinates
    private const double Lat = 27.6984037;
    private const double Lng = 85.2939889;

    // Nepal Standard Time = UTC + 5h 45m
    private static readonly TimeSpan NstOffset = TimeSpan.FromMinutes(5 * 60 + 45);

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Compute the full SolarPosition snapshot for the given UTC instant.
    /// Call this every second from the clock timer.
    /// </summary>
    public SolarPosition Calculate(DateTime utcNow)
    {
        var (alt, az, decl, ha) = GetAltAz(utcNow, Lat, Lng);
        var events = GetDailyEvents(utcNow, Lat, Lng);

        double dayFraction = -1;
        if (events.SunriseUtcMinutes.HasValue && events.SunsetUtcMinutes.HasValue)
        {
            double totalDayMin = events.SunsetUtcMinutes.Value - events.SunriseUtcMinutes.Value;
            double utcMinutes = utcNow.Hour * 60.0 + utcNow.Minute + utcNow.Second / 60.0;
            dayFraction = Math.Clamp((utcMinutes - events.SunriseUtcMinutes.Value) / totalDayMin, 0, 1);
        }

        return new SolarPosition
        {
            Altitude = alt,
            Azimuth = az,
            Declination = decl,
            HourAngle = ha,
            DayFraction = dayFraction,
            SunriseNST = UtcMinutesToNst(events.SunriseUtcMinutes),
            SolarNoonNST = UtcMinutesToNst(events.SolarNoonUtcMinutes)!.Value,
            SunsetNST = UtcMinutesToNst(events.SunsetUtcMinutes),
            GoldenHourMorningStart = UtcMinutesToNst(events.GoldenMorningStartUtcMin),
            GoldenHourMorningEnd = UtcMinutesToNst(events.GoldenMorningEndUtcMin),
            GoldenHourEveningStart = UtcMinutesToNst(events.GoldenEveningStartUtcMin),
            GoldenHourEveningEnd = UtcMinutesToNst(events.GoldenEveningEndUtcMin),
            MaxElevation = events.MaxElevation,
            DayLength = events.SunriseUtcMinutes.HasValue && events.SunsetUtcMinutes.HasValue
                ? TimeSpan.FromMinutes(events.SunsetUtcMinutes.Value - events.SunriseUtcMinutes.Value)
                : TimeSpan.Zero,
        };
    }

    // ── Core algorithm ────────────────────────────────────────────────────

    private static (double alt, double az, double decl, double ha) GetAltAz(DateTime utc, double lat, double lng)
    {
        double jd = ToJulianDay(utc);
        double T = (jd - 2451545.0) / 36525.0;

        double L0 = NormDeg(280.46646 + T * (36000.76983 + T * 0.0003032));
        double M = NormDeg(357.52911 + T * (35999.05029 - 0.0001537 * T));
        double Mr = ToRad(M);
        double C = (1.914602 - T * (0.004817 + 0.000014 * T)) * Math.Sin(Mr)
                     + (0.019993 - 0.000101 * T) * Math.Sin(2 * Mr)
                     + 0.000289 * Math.Sin(3 * Mr);
        double sunLon = L0 + C;
        double omega = 125.04 - 1934.136 * T;
        double lambda = sunLon - 0.00569 - 0.00478 * Math.Sin(ToRad(omega));
        double eps0 = 23.0 + (26.0 + (21.448 - T * (46.8150 + T * (0.00059 - T * 0.001813))) / 60.0) / 60.0;
        double epsilon = eps0 + 0.00256 * Math.Cos(ToRad(omega));

        double decl = ToDeg(Math.Asin(Math.Sin(ToRad(epsilon)) * Math.Sin(ToRad(lambda))));
        double RA = NormDeg(ToDeg(Math.Atan2(
                            Math.Cos(ToRad(epsilon)) * Math.Sin(ToRad(lambda)),
                            Math.Cos(ToRad(lambda)))));

        double GMST = NormDeg(280.46061837 + 360.98564736629 * (jd - 2451545.0)
                        + T * T * (0.000387933 - T / 38710000.0));
        double LST = NormDeg(GMST + lng);
        double ha = NormDeg(LST - RA);
        if (ha > 180) ha -= 360;
        double HAr = ToRad(ha);
        double latr = ToRad(lat);
        double declr = ToRad(decl);

        double alt = ToDeg(Math.Asin(
            Math.Sin(latr) * Math.Sin(declr) + Math.Cos(latr) * Math.Cos(declr) * Math.Cos(HAr)));
        double az = NormDeg(ToDeg(Math.Atan2(
            -Math.Sin(HAr),
            Math.Tan(declr) * Math.Cos(latr) - Math.Sin(latr) * Math.Cos(HAr))));

        return (alt, az, decl, ha);
    }

    // ── Daily events (sunrise, sunset, golden hour) ───────────────────────

    private record DailyEvents(
        double? SunriseUtcMinutes,
        double SolarNoonUtcMinutes,
        double? SunsetUtcMinutes,
        double MaxElevation,
        double? GoldenMorningStartUtcMin,
        double? GoldenMorningEndUtcMin,
        double? GoldenEveningStartUtcMin,
        double? GoldenEveningEndUtcMin
    );

    private static DailyEvents GetDailyEvents(DateTime utc, double lat, double lng)
    {
        // Use solar noon UTC as the reference time for the day
        double jd = ToJulianDay(new DateTime(utc.Year, utc.Month, utc.Day, 12, 0, 0, DateTimeKind.Utc));
        double T = (jd - 2451545.0) / 36525.0;

        double L0 = NormDeg(280.46646 + T * (36000.76983 + T * 0.0003032));
        double M = NormDeg(357.52911 + T * (35999.05029 - 0.0001537 * T));
        double Mr = ToRad(M);
        double C = (1.914602 - T * (0.004817 + 0.000014 * T)) * Math.Sin(Mr)
                      + (0.019993 - 0.000101 * T) * Math.Sin(2 * Mr)
                      + 0.000289 * Math.Sin(3 * Mr);
        double sunLon = L0 + C;
        double omega = 125.04 - 1934.136 * T;
        double lambda = sunLon - 0.00569 - 0.00478 * Math.Sin(ToRad(omega));
        double eps0 = 23.0 + (26.0 + (21.448 - T * (46.8150 + T * (0.00059 - T * 0.001813))) / 60.0) / 60.0;
        double epsilon = eps0 + 0.00256 * Math.Cos(ToRad(omega));
        double decl = ToDeg(Math.Asin(Math.Sin(ToRad(epsilon)) * Math.Sin(ToRad(lambda))));

        // Equation of time (minutes)
        double y = Math.Pow(Math.Tan(ToRad(epsilon / 2)), 2);
        double eot = 4.0 * ToDeg(
            y * Math.Sin(2 * ToRad(L0))
            - 2 * Math.Sin(Mr)
            + 4 * Math.Sin(Mr) * y * Math.Cos(2 * ToRad(L0))
            - 0.5 * y * y * Math.Sin(4 * ToRad(L0))
            - 1.25 * Math.Sin(2 * Mr));

        // Solar noon in UTC minutes from midnight
        double solarNoonUtc = 720.0 - 4.0 * lng - eot;

        // Hour angle for sunrise/sunset (atmospheric refraction correction: -0.833°)
        double cosHA = (Math.Cos(ToRad(90.833)) - Math.Sin(ToRad(lat)) * Math.Sin(ToRad(decl)))
                     / (Math.Cos(ToRad(lat)) * Math.Cos(ToRad(decl)));

        double? sunriseMin = null, sunsetMin = null;
        if (cosHA is >= -1 and <= 1)
        {
            double hasr = ToDeg(Math.Acos(cosHA));
            sunriseMin = solarNoonUtc - 4.0 * hasr;
            sunsetMin = solarNoonUtc + 4.0 * hasr;
        }

        double maxElev = 90.0 - Math.Abs(lat - decl);

        // Golden / blue hour: find HA for target elevations -6° and +6°
        static double? HaMinutes(double elevDeg, double latDeg, double declDeg, double noon)
        {
            double cosH = (Math.Sin(ToRad(elevDeg)) - Math.Sin(ToRad(latDeg)) * Math.Sin(ToRad(declDeg)))
                        / (Math.Cos(ToRad(latDeg)) * Math.Cos(ToRad(declDeg)));
            if (cosH is < -1 or > 1) return null;
            return ToDeg(Math.Acos(cosH)) * 4.0;
        }

        double? ha6 = HaMinutes(6, lat, decl, solarNoonUtc);
        double? ha_6 = HaMinutes(-6, lat, decl, solarNoonUtc);

        return new DailyEvents(
            SunriseUtcMinutes: sunriseMin,
            SolarNoonUtcMinutes: solarNoonUtc,
            SunsetUtcMinutes: sunsetMin,
            MaxElevation: maxElev,
            GoldenMorningStartUtcMin: ha_6.HasValue ? solarNoonUtc - ha_6.Value : null,
            GoldenMorningEndUtcMin: ha6.HasValue ? solarNoonUtc - ha6.Value : null,
            GoldenEveningStartUtcMin: ha6.HasValue ? solarNoonUtc + ha6.Value : null,
            GoldenEveningEndUtcMin: ha_6.HasValue ? solarNoonUtc + ha_6.Value : null
        );
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static TimeOnly? UtcMinutesToNst(double? utcMinutes)
    {
        if (!utcMinutes.HasValue) return null;
        double nstMin = (utcMinutes.Value + NstOffset.TotalMinutes) % 1440;
        if (nstMin < 0) nstMin += 1440;
        int h = (int)(nstMin / 60);
        int m = (int)(nstMin % 60);
        int s = (int)((nstMin % 1) * 60);
        h %= 24;
        return new TimeOnly(h, m, s);
    }

    private static double ToJulianDay(DateTime utc)
        => utc.ToOADate() + 2415018.5;

    private static double ToRad(double deg) => deg * Math.PI / 180.0;
    private static double ToDeg(double rad) => rad * 180.0 / Math.PI;
    private static double NormDeg(double d) => ((d % 360) + 360) % 360;
}
