namespace CollabsKus.BlazorWebAssembly.Settings;

internal static class OptionsGuard
{
   public static bool IsHttpUrl(string? value) =>
      Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
      && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

   public static bool IsRelativePath(string? value) =>
      !string.IsNullOrWhiteSpace(value)
      && Uri.TryCreate(value, UriKind.Relative, out _)
      && !value.StartsWith('/');
}
