namespace CollabsKus.BlazorWebAssembly.Models;

public class MoonPhase
{
   public string Name { get; set; } = string.Empty;
   public string Icon { get; set; } = string.Empty;
   public double Illumination { get; set; }
   public double Age { get; set; }

   public double DaysUntilNewMoon { get; set; }
   public double DaysUntilFullMoon { get; set; }
   public double DaysSinceNewMoon { get; set; }
   public double DaysSinceFullMoon { get; set; }

   public int TithiNumber { get; set; }
   public string TithiName { get; set; } = string.Empty;
   public string Paksha { get; set; } = string.Empty;

   public bool IsWaxing => DaysUntilFullMoon < DaysUntilNewMoon;
}
