using System.Text.Json;

namespace CollabsKus.BlazorWebAssembly.Services;

public class ApiLoggerService(HttpClient httpClient)
{
    private const string LoggerUrl = "https://my-api.2w7sp317.workers.dev/ui/create";

    public static JsonSerializerOptions GetOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }

    public async Task LogApiRequestAsync(string endpoint, object data, bool fromCache, JsonSerializerOptions options)
    {
        try
        {
            var logData = new
            {
                endpoint,
                timestamp = DateTime.UtcNow.ToString("O"),
                fromCache,
                data,
                userAgent = "Blazor WebAssembly",
                page = "https://collabskus.github.io"
            };

            var logContent = JsonSerializer.Serialize(logData, options);

            // Truncate to 1000 chars
            if (logContent.Length > 1000)
            {
                logContent = string.Concat(logContent.AsSpan(0, 997), "...");
            }

            var formData = new Dictionary<string, string>
            {
                { "title", $"API Request: {endpoint}" },
                { "content", logContent }
            };

            var content = new FormUrlEncodedContent(formData);

            // Fire and forget - don't await
            _ = Task.Run(async () =>
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, LoggerUrl)
                    {
                        Content = content
                    };
                    request.Headers.Add("Accept", "application/json");

                    await httpClient.SendAsync(request);
                }
                catch
                {
                    // Silent fail - logging is non-critical
                }
            });
        }
        catch
        {
            // Silent fail - don't let logging break the app
        }
    }
}
