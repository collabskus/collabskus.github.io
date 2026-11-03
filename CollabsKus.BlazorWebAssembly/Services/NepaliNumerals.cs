using System.Globalization;
using System.Text;

namespace CollabsKus.BlazorWebAssembly.Services;

public static class NepaliNumerals
{
   private const char DevanagariZero = '\u0966';

   public static string ToNepaliDigits(int number)
   {
      string digits = number.ToString("D2", CultureInfo.InvariantCulture);
      var builder = new StringBuilder(digits.Length);
      foreach (char digit in digits)
      {
         if (char.IsAsciiDigit(digit))
         {
            builder.Append((char)(DevanagariZero + (digit - '0')));
         }
      }
      return builder.ToString();
   }
}
