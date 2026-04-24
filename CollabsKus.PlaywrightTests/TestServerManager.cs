using System.Diagnostics;

namespace CollabsKus.PlaywrightTests;

/// <summary>
/// Shared dev server lifecycle for Playwright test classes.
/// Uses ref-counting so multiple classes can call Acquire/Release
/// independently; the process starts on first Acquire and stops
/// on the last Release.
/// </summary>
internal static class TestServerManager
{
    internal const string DefaultBaseUrl = "http://localhost:5267";

    private static Process? _process;
    private static int _refCount;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public static async Task AcquireAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _refCount++;
            if (_refCount > 1) return;
            await StartAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public static async Task ReleaseAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _refCount--;
            if (_refCount > 0) return;
            await StopAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static async Task StartAsync()
    {
        var baseDir = AppContext.BaseDirectory;
        var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
        var blazorProject = Path.Combine(
            projectRoot, "CollabsKus.BlazorWebAssembly", "CollabsKus.BlazorWebAssembly.csproj");

        if (!File.Exists(blazorProject))
            throw new FileNotFoundException(
                $"Blazor project not found at: {blazorProject} (resolved from {baseDir})");

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{blazorProject}\" --urls {DefaultBaseUrl} --no-launch-profile",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow.AddSeconds(120);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await httpClient.GetAsync(DefaultBaseUrl);
                if (response.IsSuccessStatusCode) return;
            }
            catch { }

            if (_process.HasExited)
                throw new InvalidOperationException(
                    $"Dev server exited with code {_process.ExitCode} before becoming ready.");

            await Task.Delay(1000);
        }

        throw new TimeoutException($"Dev server did not respond at {DefaultBaseUrl} within 120 s.");
    }

    private static async Task StopAsync()
    {
        if (_process == null || _process.HasExited) return;
        try
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync();
        }
        catch { }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }
}
