# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Blazor WebAssembly (.NET 10) single-page app displaying a Kathmandu Calendar & Time with Bikram Sambat calendar, real-time clock, moon phase calculations, and solar position tracking. Deploys automatically to GitHub Pages and works offline as a PWA after the first visit.

## Commands

```bash
dotnet run --project CollabsKus.BlazorWebAssembly
dotnet build CollabsKus.BlazorWebAssembly/CollabsKus.BlazorWebAssembly.csproj --configuration Release
dotnet publish CollabsKus.BlazorWebAssembly/CollabsKus.BlazorWebAssembly.csproj -c Release -o publish --no-build
dotnet test CollabsKus.Tests/CollabsKus.Tests.csproj
dotnet test CollabsKus.PlaywrightTests/CollabsKus.PlaywrightTests.csproj
```

Dev server runs at `http://localhost:5267` / `https://localhost:7212`.

## Architecture

**Configuration**: no magic constants. All tunables live in `wwwroot/appsettings.json`, bound to classes in `Settings/` that implement `IAppOptions` (static `SectionName` + `Validate()`), registered with `AddAppOptions<T>()`. Add new settings the same way and extend `AppSettingsTests`.

**Component hierarchy**: `App.razor` → `MainLayout.razor` → `Home.razor` → `TimeDisplay`, `DateCards`, `MoonDisplay`, `Sundisplay`, `CalendarGrid`. Each component has a paired `.razor.css`.

**`Home.razor` is the state hub**: renders immediately from local calculations, then runs cancellable `PeriodicTimer` loops (clock tick, clock sync check, calendar check) configured by `Refresh`. Refreshes are gated against overlap and also triggered by online/visibility events and Kathmandu day rollover.

**Services** (registered in `Program.cs`):
- `KathmanduCalendarService` — calendar and clock offset; checks memory, then `ILocalCache`, then the API; projects or marks saved calendars stale when offline.
- `LocalCache` / `BrowserKeyValueStore` — versioned, timestamped JSON in `localStorage` via `window.appStorage`.
- `ClockOffsetCalculator`, `CalendarProjection` — pure, unit-tested helpers.
- `MoonPhaseService` — static Meeus calculator; always pass true UTC.
- `SolarPositionService` — stateless; takes a `GeoLocation`.
- `GeolocationService`, `ApiLoggerService` (`IApiLogger`), `BlogService`, `TelemetryService`.

**JavaScript**: `js/interop.js` (page monitor, geolocation, storage, sun canvas), `js/pwa.js` (service worker registration and update banner), `js/telemetry.js` (Application Insights module).

**Testing**: TUnit with async assertions. Use `ManualTimeProvider`, `InMemoryKeyValueStore`, `StubHttpMessageHandler`, `RecordingApiLogger` from `CollabsKus.Tests/Support`.

**Package versions** are managed centrally in `Directory.Packages.props`.

## Deployment

`.github/workflows/deploy.yml` builds, publishes and deploys to GitHub Pages on push to `main`/`master`, setting the base path for user vs project sites.
