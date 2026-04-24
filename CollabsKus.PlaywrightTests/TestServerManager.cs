using System.Diagnostics;
using System.Net.NetworkInformation;

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
    private const int Port = 5267;

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
        // Kill any stale server left over from a previous aborted test run so it
        // doesn't accept our port-poll and serve a different version of the app.
        KillPortOccupant();

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
                // --no-hot-reload prevents the server from restarting when source
                // files change during a test run, which would cause intermittent failures.
                Arguments = $"run --project \"{blazorProject}\" --urls {DefaultBaseUrl} --no-launch-profile --no-hot-reload",
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

    /// <summary>
    /// Kills any process currently listening on the server port.
    /// Prevents a stale dotnet run from a previous test run from
    /// masquerading as a healthy server.
    /// </summary>
    private static void KillPortOccupant()
    {
        try
        {
            // Use IPGlobalProperties to find the TCP listener PID on our port.
            // On Linux/macOS this works via /proc; on Windows it uses Win32 APIs.
            var listeners = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners();

            var isOccupied = listeners.Any(ep => ep.Port == Port);
            if (!isOccupied) return;

            // Fall back to netstat-style process enumeration via dotnet Process API.
            // We can't get the PID from IPGlobalProperties directly, so look for
            // any dotnet process that owns the port using platform commands.
            KillPortOccupantViaPlatformCommand();
        }
        catch
        {
            // Non-fatal — if we can't kill the occupant the server start will fail
            // with a clear "address in use" error.
        }
    }

    private static void KillPortOccupantViaPlatformCommand()
    {
        string? cmd;
        string args;

        if (OperatingSystem.IsWindows())
        {
            // netstat -ano on Windows lists PID for each connection
            cmd = "cmd.exe";
            args = $"/c for /f \"tokens=5\" %a in ('netstat -ano ^| findstr :{Port}.*LISTENING') do taskkill /PID %a /F";
        }
        else
        {
            cmd = "/bin/sh";
            args = $"-c \"fuser -k {Port}/tcp 2>/dev/null || true\"";
        }

        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = cmd,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        p?.WaitForExit(5000);

        // Give the OS a moment to release the port.
        Thread.Sleep(500);
    }
}
