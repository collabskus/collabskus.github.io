using Microsoft.Extensions.Options;

namespace CollabsKus.BlazorWebAssembly.Settings;

public sealed class AppOptionsValidator<T> : IValidateOptions<T> where T : class, IAppOptions
{
   public ValidateOptionsResult Validate(string? name, T options)
   {
      List<string> failures = [.. options.Validate().Select(error => $"{T.SectionName}: {error}")];
      return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
   }
}
