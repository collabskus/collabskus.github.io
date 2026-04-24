# CollabsKus — Kathmandu Calendar & Time

A Blazor WebAssembly (.NET 10) single-page application that displays real-time Kathmandu information: clock, Bikram Sambat calendar, moon phase, solar position, and a blog. Deployed to GitHub Pages at [collabskus.github.io](https://collabskus.github.io).

> **Note:** This repository contains code generated with the assistance of LLMs such as Claude (Anthropic). Human review and testing is applied throughout.

---

## Features

- Real-time clock synchronized to Kathmandu time (UTC+5:45) via external API
- Bikram Sambat calendar with Gregorian cross-reference
- Moon phase calculation (astronomical algorithms, no external library)
- Solar position tracker with altitude, azimuth, sunrise/sunset, and canvas visualization
- User geolocation support for local sun tracking
- Blog at `/blog` and `/blog/{slug}` with markdown posts
- Offline-capable via service worker
- Responsive, no CSS framework, no external JS libraries

---

## Project structure

```
CollabsKus.BlazorWebAssembly/
├── Components/             # Reusable UI components
│   ├── CalendarGrid.razor
│   ├── DateCards.razor
│   ├── MoonDisplay.razor
│   ├── Sundisplay.razor
│   └── TimeDisplay.razor
├── Layout/
│   └── MainLayout.razor
├── Models/
│   ├── BlogModels.cs       # Blog manifest, post, author models
│   ├── CalendarResponse.cs
│   ├── MoonPhase.cs
│   ├── SolarPosition.cs
│   └── TimeResponse.cs
├── Pages/
│   ├── BlogDetail.razor    # /blog/{slug}
│   ├── BlogList.razor      # /blog
│   └── Home.razor          # / — state hub, owns all timers and data
├── Services/
│   ├── ApiLoggerService.cs
│   ├── BlogService.cs      # Fetches manifest + markdown, parses with Markdig
│   ├── KathmanduCalendarService.cs
│   ├── MoonPhaseService.cs
│   └── SolarPositionService.cs
└── wwwroot/
    ├── blog/
    │   ├── authors/        # {id}.json author metadata
    │   ├── posts/          # {slug}.md markdown posts
    │   └── manifest.json   # index of published posts
    ├── css/app.css
    ├── 404.html            # GitHub Pages SPA routing fallback
    └── index.html

CollabsKus.Tests/           # TUnit unit tests (services, math, parsing)
CollabsKus.PlaywrightTests/ # Playwright E2E tests (Chromium, headless)

scripts/
└── update-blog-manifest.py # Publishes posts whose date has arrived

.github/workflows/
├── deploy.yml              # Build and deploy to GitHub Pages on push to main/master
├── publish-blog.yml        # Daily scheduled: publishes due blog posts
└── test.yml                # Run all tests on every push and PR
```

---

## Commands

```bash
# Run dev server (http://localhost:5267 or https://localhost:7212)
dotnet run --project CollabsKus.BlazorWebAssembly

# Build
dotnet build CollabsKus.BlazorWebAssembly/CollabsKus.BlazorWebAssembly.csproj

# Unit tests
dotnet test CollabsKus.Tests/CollabsKus.Tests.csproj

# E2E tests (requires a running app, or set BASE_URL to skip server management)
dotnet test CollabsKus.PlaywrightTests/CollabsKus.PlaywrightTests.csproj
BASE_URL=http://localhost:5267 dotnet test CollabsKus.PlaywrightTests/CollabsKus.PlaywrightTests.csproj
```

---

## Architecture

**`Home.razor` is the state hub.** It owns the data and runs three timers: a 10ms clock, a 1-hour time API sync, and a 24-hour calendar refresh. It uses JS interop for tab visibility detection and geolocation, then passes data down to all child components as parameters.

**Services** are registered in `Program.cs`:

| Service | Role |
|---|---|
| `KathmanduCalendarService` | API client with TTL caching (calendar 1h, time 5min). Tracks server time offset so the clock stays accurate between syncs. |
| `MoonPhaseService` | Stateless astronomical calculator (Julian Day, lunar age, illumination, tithi/paksha). |
| `SolarPositionService` | Singleton computing altitude, azimuth, sunrise/sunset, solar noon, golden hour for any lat/lon. |
| `BlogService` | Fetches `blog/manifest.json`, individual `.md` files, and author JSON. Renders markdown with Markdig. |
| `ApiLoggerService` | Fire-and-forget telemetry to Cloudflare Workers. |

**Blog workflow:** Posts live as `.md` files in `wwwroot/blog/posts/`. A daily GitHub Action runs `scripts/update-blog-manifest.py`, which adds any post whose date has arrived to `manifest.json` and commits the change, triggering a new deployment.

**GitHub Pages SPA routing:** `wwwroot/404.html` redirects deep links (e.g. `/blog/hello-world`) to `/?/blog/hello-world`. A script in `index.html` restores the real URL before Blazor initialises. Once the service worker is installed it handles navigation directly from cache.

---

## Testing

Unit tests cover service logic (calendar conversions, moon/solar math, blog frontmatter parsing, markdown rendering). E2E tests cover the full rendered app including the home page, all blog routes, navigation, and edge cases (geolocation, unknown slugs, moon live indicator).

The `test.yml` workflow runs both suites on every push to any branch and on every pull request.

---

## Deployment

Pushing to `main` or `master` triggers `deploy.yml`, which builds, publishes, and deploys to GitHub Pages. The workflow detects whether the repository is a user/org site or a project site and sets the correct `<base href>` automatically.
