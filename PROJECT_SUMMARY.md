# Project Summary

Kathmandu Calendar & Time — Blazor WebAssembly (.NET 10) PWA on GitHub Pages. See `README.md` for features, configuration and offline behaviour, and `CLAUDE.md` for architecture.

## Projects

- `CollabsKus.BlazorWebAssembly` — the app
- `CollabsKus.Tests` — TUnit unit tests
- `CollabsKus.PlaywrightTests` — Playwright end-to-end tests

## Key points

- All tunables in `wwwroot/appsettings.json`, bound to validated options in `Settings/`
- API data cached in `localStorage`; checked before every request
- Service worker caches the app for offline use
- Moon and sun positions computed locally (Meeus)
- Clock tick 200 ms, clock sync and calendar checks every 15 minutes (configurable)
