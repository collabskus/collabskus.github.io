# Kathmandu Calendar & Time

Blazor WebAssembly (.NET 10) app for Kathmandu, Nepal: live Nepal time, Bikram Sambat calendar, moon phase with tithi/paksha, and a sun tracker. Hosted for free on GitHub Pages at https://collabskus.github.io and installable as an offline-capable PWA.

## Features

- Live clock in Nepal time with Devanagari digits
- Bikram Sambat month grid; tap a day for its sunrise and sunset
- Moon phase, illumination, age, tithi and paksha (Meeus, computed locally)
- Sun altitude, azimuth, sunrise, sunset, golden hours for Kathmandu and, with permission, your location
- Blog rendered from Markdown
- Works offline after the first visit

## Offline and resilience

| Data | Source | Offline behaviour |
|------|--------|-------------------|
| App shell, assets, blog | Service worker cache (`service-worker.published.js`) | Served from cache; a banner offers reload when a new version is deployed |
| Calendar month | Calendar API, cached in `localStorage` per Kathmandu date | Saved month is reused; "today" is advanced locally while inside the same month, otherwise the saved month is shown with a notice |
| Clock offset | Time API, cached in `localStorage` (`CalendarApi:ClockOffsetCacheMinutes`) | Falls back to the fixed UTC offset from `Location:UtcOffsetMinutes` |
| Moon and sun | Calculated in C# | Always available |

`localStorage` is checked before any API request. Connectivity changes, tab visibility and Kathmandu midnight trigger a refresh. The footer shows an Offline badge when the browser is offline.

## Configuration

Every tunable value lives in `CollabsKus.BlazorWebAssembly/wwwroot/appsettings.json` and is bound to a validated options class in `Settings/`:

| Section | Class | Purpose |
|---------|-------|---------|
| `Location` | `LocationOptions` | Home location name, coordinates, UTC offset |
| `CalendarApi` | `CalendarApiOptions` | API URLs, request timeout, clock offset cache lifetime |
| `Refresh` | `RefreshOptions` | Clock tick (ms), clock sync and calendar check intervals |
| `LocalStorage` | `LocalStorageOptions` | Key prefix and schema version (bump to invalidate saved data) |
| `Geolocation` | `GeolocationOptions` | Browser geolocation settings and label |
| `ApiLogger` | `ApiLoggerOptions` | Request logging endpoints and limits |
| `Blog` | `BlogOptions` | Blog paths and manifest cache time |
| `ApplicationInsights` | `TelemetryOptions` | Connection string, SDK URL, cloud role, queue size |

Invalid configuration fails fast on first use, except telemetry which disables itself. `AppSettingsTests` validates the real `appsettings.json` in CI.

## Project layout

```
CollabsKus.BlazorWebAssembly/
├── Components/   CalendarGrid, DateCards, MoonDisplay, Sundisplay, TimeDisplay
├── Layout/       MainLayout
├── Models/       API models, CalendarSnapshot, GeoLocation, MoonPhase, SolarPosition
├── Pages/        Home, BlogList, BlogDetail, NotFound
├── Services/     KathmanduCalendarService, LocalCache, ClockOffsetCalculator, CalendarProjection,
│                 MoonPhaseService, SolarPositionService, GeolocationService, ApiLoggerService,
│                 BlogService, TelemetryService, NepaliNumerals
├── Settings/     Options classes and validation
└── wwwroot/      appsettings.json, index.html, css/, js/interop.js, js/pwa.js, js/telemetry.js,
                  service-worker(.published).js, manifest.webmanifest, blog/
CollabsKus.Tests/            TUnit unit tests
CollabsKus.PlaywrightTests/  Playwright end-to-end tests
```

## Commands

```bash
dotnet run --project CollabsKus.BlazorWebAssembly
dotnet test CollabsKus.Tests/CollabsKus.Tests.csproj
dotnet test CollabsKus.PlaywrightTests/CollabsKus.PlaywrightTests.csproj
```

The dev server listens on http://localhost:5267 and https://localhost:7212. The development service worker is a no-op; offline support only applies to published builds.

## Deployment

`.github/workflows/deploy.yml` builds, publishes and deploys to GitHub Pages on push to `main`/`master`, rewriting `<base href>` for project sites. The service worker derives its base path from its registration scope.

## Notes

- Sunrise and sunset are astronomical (flat horizon, standard refraction); valley terrain makes visible times differ.
- Calendar projection assumes the API tags days outside the current month as `pre`/`next` and the current day as `today`.
