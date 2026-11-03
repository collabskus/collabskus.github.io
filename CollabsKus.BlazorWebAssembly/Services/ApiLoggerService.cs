using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CollabsKus.BlazorWebAssembly.Settings;
using Microsoft.Extensions.Options;

namespace CollabsKus.BlazorWebAssembly.Services;

public sealed class ApiLoggerService(HttpClient httpClient, IOptions<ApiLoggerOptions> options, TimeProvider timeProvider) : IApiLogger
{
   private const string TruncationSuffix = "...";

   private static readonly JsonSerializerOptions SerializerOptions = new()
   {
      WriteIndented = true
   };

   private readonly ApiLoggerOptions _options = options.Value;

   public void LogApiRequest(string endpoint, object? data, bool fromCache)
   {
      if (!_options.Enabled)
      {
         return;
      }

      string logContent;
      try
      {
         logContent = JsonSerializer.Serialize(new
         {
            endpoint,
            timestamp = timeProvider.GetUtcNow().UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            fromCache,
            data,
            userAgent = _options.UserAgentLabel,
            page = _options.PageUrl
         }, SerializerOptions);
      }
      catch (Exception ex) when (ex is JsonException or NotSupportedException)
      {
         Debug.WriteLine($"ApiLoggerService: failed to build log payload: {ex.Message}");
         return;
      }

      string title = $"API Request: {endpoint}";

      var notesBody = new FormUrlEncodedContent(new Dictionary<string, string>
      {
         ["title"] = title,
         ["content"] = Truncate(logContent, _options.MaxContentLength)
      });

      var vaultBody = new StringContent(
         JsonSerializer.Serialize(new Dictionary<string, string>
         {
            ["payload"] = Truncate($"{title}\n{logContent}", _options.MaxContentLength)
         }),
         Encoding.UTF8,
         "application/json");

      _ = PostAsync(_options.NotesUrl, notesBody);
      _ = PostAsync(_options.VaultUrl, vaultBody);
   }

   private async Task PostAsync(string url, HttpContent content)
   {
      try
      {
         using var request = new HttpRequestMessage(HttpMethod.Post, url)
         {
            Content = content
         };
         request.Headers.Add("Accept", "application/json");

         using var response = await httpClient.SendAsync(request);

         if (!response.IsSuccessStatusCode)
         {
            Debug.WriteLine($"ApiLoggerService: {url} returned {(int)response.StatusCode}");
         }
      }
      catch (Exception ex)
      {
         Debug.WriteLine($"ApiLoggerService: post to {url} failed: {ex.Message}");
      }
   }

   private static string Truncate(string value, int maxLength)
   {
      if (value.Length <= maxLength)
      {
         return value;
      }

      int cut = maxLength - TruncationSuffix.Length;

      if (cut > 0 && char.IsHighSurrogate(value[cut - 1]))
      {
         cut--;
      }

      return string.Concat(value.AsSpan(0, cut), TruncationSuffix);
   }
}
