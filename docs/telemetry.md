# Telemetry (Azure Application Insights)

## Design

- `wwwroot/appsettings.json` → `ApplicationInsights:ConnectionString` is the single source of truth. It is an ingestion key, not a secret.
- `Program.cs` builds a `TelemetryOptions` (connection string + Blazor environment) and registers `ITelemetryService` → `TelemetryService` and `TelemetryLoggerProvider` as singletons.
- `TelemetryService` lazily imports the ES module `wwwroot/js/telemetry.js` through `IJSRuntime` and never throws. With no connection string it is a no-op.
- `telemetry.js` downloads the official SDK from `https://js.monitor.azure.com/scripts/b/ai.3.gbl.min.js`, creates `Microsoft.ApplicationInsights.ApplicationInsights`, tracks the initial page view, and replays calls queued before the SDK was ready (bounded queue).
- Every envelope gets cloud role `collabskus-web` and a custom dimension `environment` (`Production` on GitHub Pages, `Development` locally / in E2E runs).
- `index.html` contains no Application Insights code.

## What is collected

| Source | Telemetry |
|---|---|
| SDK startup | initial `pageViews` row, browser timings, `window.onerror`, fetch/XHR dependencies |
| `NavigationManager.LocationChanged` | `pageViews` for client-side route changes |
| `TelemetryLoggerProvider` | `ILogger` Error/Critical → `exceptions` (with exception) or `traces` |
| `AppDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException` | `exceptions` |

## Verifying

1. DevTools → Network: `ai.3.gbl.min.js` returns 200, then POSTs to `https://eastus-8.in.applicationinsights.azure.com/v2/track` return 200.
2. Azure portal → Application Insights → Logs (allow a few minutes of ingestion latency):

```kusto
union pageViews, exceptions, traces, dependencies
| where timestamp > ago(1h)
| project timestamp, itemType, name, cloud_RoleName, environment = tostring(customDimensions.environment)
| order by timestamp desc
```

Live Metrics does not show browser SDK data.

## Troubleshooting

- Requests to `js.monitor.azure.com` or `*.applicationinsights.azure.com` blocked: an ad/tracking blocker is active. Test in a clean profile.
- `/v2/track` returns 401/403: the resource has local authentication disabled; browser ingestion needs it enabled.
- Old behaviour after deploy: the published service worker serves cached assets. Close all tabs of the site or use DevTools → Application → Service workers → Update.
- To silence telemetry for an environment, add `wwwroot/appsettings.{Environment}.json` with an empty `ApplicationInsights:ConnectionString`.
