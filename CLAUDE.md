# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Blazor WebAssembly (.NET 10) single-page app displaying a Kathmandu Calendar & Time with Bikram Sambat calendar, real-time clock, moon phase calculations, and solar position tracking. Deploys automatically to GitHub Pages.

## Commands

```bash
# Develop
dotnet run --project CollabsKus.BlazorWebAssembly

# Build
dotnet build CollabsKus.BlazorWebAssembly/CollabsKus.BlazorWebAssembly.csproj --configuration Release

# Publish
dotnet publish CollabsKus.BlazorWebAssembly/CollabsKus.BlazorWebAssembly.csproj -c Release -o publish --no-build

# Run unit tests
dotnet test CollabsKus.Tests/CollabsKus.Tests.csproj

# Run E2E tests (requires running app)
dotnet test CollabsKus.PlaywrightTests/CollabsKus.PlaywrightTests.csproj
```

Dev server runs at `https://localhost:7212`.

## Architecture

**Component hierarchy**: `App.razor` → `MainLayout.razor` → `Home.razor` → UI components (`TimeDisplay`, `DateCards`, `MoonDisplay`, `Sundisplay`, `CalendarGrid`). Each component has a paired `.razor.css` for scoped styles.

**`Home.razor` is the state hub** — it owns all data, runs three concurrent timers (10ms clock, 1-hour time API sync, 24-hour calendar refresh), and passes data down via parameters. It uses JS interop for tab visibility and geolocation, and implements `IAsyncDisposable` for cleanup.

**Services** (registered in `Program.cs`):
- `KathmanduCalendarService` — API client with TTL caching (calendar: 1 hour, time: 5 min). Calculates server time offset so the clock stays accurate between API calls.
- `MoonPhaseService` — Stateless astronomical calculator using Jean Meeus "Astronomical Algorithms" (Julian Day Number, lunar age, illumination, tithi/paksha).
- `SolarPositionService` — Stateless singleton computing altitude, azimuth, sunrise/sunset, solar noon, and golden hour windows for a given lat/lon.
- `ApiLoggerService` — Fire-and-forget telemetry to Cloudflare Workers; silent on failure.

**No external JS libraries or CSS frameworks** — all styling is hand-written, all astronomical math is in C#, and JS interop is limited to geolocation and visibility detection.

**Package versions** are managed centrally in `Directory.Packages.props` (do not specify versions in individual `.csproj` files).

## Deployment

CI/CD is in `.github/workflows/deploy.yml`. On push to `main`/`master` it builds, publishes, and deploys to GitHub Pages. The workflow detects whether it's a user site or project site to set the correct base path for Blazor's routing.
