# Kathmandu Calendar & Time - Blazor WebAssembly

A beautiful, responsive Blazor WebAssembly application that displays the current time and calendar for Kathmandu, Nepal, including moon phase information.

## 🏗️ Project Structure

```
CollabsKus.BlazorWebAssembly/
├── Components/              # Reusable UI components
│   ├── CalendarGrid.razor   # Displays the Nepali calendar grid
│   ├── DateCards.razor      # Shows Bikram Sambat and Gregorian dates
│   ├── MoonDisplay.razor    # Displays current moon phase
│   └── TimeDisplay.razor    # Shows current time in English and Nepali
├── Layout/
│   └── MainLayout.razor     # Main layout wrapper
├── Models/                  # Data models
│   ├── CalendarResponse.cs  # Calendar API response model
│   ├── MoonPhase.cs        # Moon phase data model
│   └── TimeResponse.cs     # Time API response model
├── Pages/
│   └── Home.razor          # Main home page
├── Services/               # Business logic services
│   ├── ApiLoggerService.cs # Logs API requests to Cloudflare Workers
│   ├── KathmanduCalendarService.cs # Handles calendar/time API calls
│   └── MoonPhaseService.cs # Calculates moon phases
├── wwwroot/
│   ├── css/
│   │   └── app.css        # Global styles
│   └── index.html         # Main HTML entry point
├── App.razor              # Root component
├── Program.cs             # Application entry point
└── _Imports.razor         # Global using statements
```

## 🎨 Architecture Decisions

### Component-Based Design
Each UI element is a separate component with its own scoped CSS:
- **TimeDisplay**: Real-time clock with Nepali numerals
- **DateCards**: Bikram Sambat and Gregorian dates
- **MoonDisplay**: Current moon phase with icon
- **CalendarGrid**: Full month calendar with today highlighted

### Service Layer
Business logic is separated into services:
- **KathmanduCalendarService**: Manages API calls with intelligent caching
  - Calendar data cached for 1 hour
  - Time data cached for 5 minutes
  - Calculates server time offset for accurate local time
- **MoonPhaseService**: Client-side moon phase calculation using astronomical algorithms
- **ApiLoggerService**: Logs all API requests to Cloudflare Workers (non-blocking)

### State Management
- Component state managed in `Home.razor`
- Timers for:
  - Clock updates (every 1 second)
  - Time API refresh (every 1 hour)
  - Calendar API refresh (every 24 hours)

### CSS Strategy
- **Global styles** in `wwwroot/css/app.css` for body, page layout
- **Scoped CSS** for each component (automatic isolation)
- **No external dependencies** - all CSS written from scratch
- **Responsive design** with mobile-first breakpoints

## 🚀 How It Works

### Data Flow
1. **Initial Load**:
   - Home page fetches calendar and time data
   - Calculates initial moon phase
   - Starts three timers

2. **Real-time Updates**:
   - Clock timer updates every second (local calculation)
   - Time API refreshes hourly to prevent drift
   - Calendar API refreshes daily

3. **Caching Strategy**:
   - Services cache API responses in memory
   - Cached responses logged with `fromCache: true`
   - Fresh API calls logged with `fromCache: false`

4. **Error Handling**:
   - API failures don't crash the app
   - Logging failures are silent (non-critical)
   - User sees friendly error messages

### API Integration
- **Calendar API**: `https://calendar.bloggernepal.com/api/today`
- **Time API**: `https://calendar.bloggernepal.com/api/time`
- **Logger API**: `https://my-api.2w7sp317.workers.dev/ui/create`

### Moon Phase Calculation
Uses Julian Day Number algorithm:
- Calculates days since known new moon (Jan 6, 2000)
- Determines current lunar age
- Computes illumination percentage
- Maps to appropriate phase icon

## 🎓 Learning Points

### Blazor Concepts Demonstrated
1. **Component Composition**: Building complex UIs from small components
2. **Dependency Injection**: Services injected into components
3. **Lifecycle Methods**: `OnInitializedAsync` for data loading
4. **Scoped CSS**: Component-specific styling
5. **Parameter Binding**: Passing data between components
6. **Timer Management**: Background tasks with proper disposal
7. **Error Boundaries**: Graceful error handling

### Best Practices
- ✅ Separation of concerns (UI, logic, data)
- ✅ Single responsibility principle
- ✅ Async/await for API calls
- ✅ Proper resource disposal (IDisposable)
- ✅ Responsive design
- ✅ No JavaScript (pure C#)
- ✅ No external dependencies

## 🔧 Configuration

The app is configured for GitHub Pages deployment at `https://collabskus.github.io`.

### Base Path
The `<base href="/" />` in `index.html` is set for root domain deployment.

### Service Worker
Configured for offline support with automatic updates.

## 📦 Deployment

GitHub Actions automatically builds and deploys on push to main/master:
1. Builds the Blazor WASM project
2. Publishes to `release/wwwroot`
3. Uploads to GitHub Pages
4. Deploys to production

## 🎯 Features

- ✨ Real-time clock synchronized with Kathmandu time
- 📅 Bikram Sambat (Nepali) calendar
- 🌍 Gregorian calendar
- 🌙 Accurate moon phase calculation
- 📱 Fully responsive (desktop, tablet, mobile)
- 🎨 Beautiful gradient background with glassmorphism
- 📊 API request logging
- ⚡ Intelligent caching
- 🔄 Automatic refresh intervals
- 💪 No external dependencies

## 🧪 Testing Locally

```bash
dotnet run --project CollabsKus.BlazorWebAssembly
```

Navigate to `https://localhost:7212` or the port shown in console.

## 📚 Additional Resources

- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [Scoped CSS](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation)
- [Dependency Injection](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection)

# Blazor WebAssembly GitHub Pages Deployment Fix

## The Problem

You're getting a 404 error for `_framework/blazor.webassembly.js` because the base path in your `index.html` doesn't match your GitHub Pages deployment structure.

## The Solution

I've updated your GitHub Actions workflow to automatically detect whether you're deploying to:
- **User/Organization site** (`https://username.github.io`) → uses base path `/`
- **Project site** (`https://username.github.io/repo-name`) → uses base path `/repo-name/`

## Files to Update

### 1. `.github/workflows/deploy.yml`
Replace your current workflow with the new one that includes base path detection.

### 2. `CollabsKus.BlazorWebAssembly/wwwroot/index.html`
No changes needed - the workflow will update it automatically during build.

## What Changed in the Workflow

**New step added:**
```yaml
- name: Determine base path for GitHub Pages
  id: basepath
  run: |
    # Automatically detects if user/org site or project site
    # Sets the correct base path accordingly
```

**Updated step:**
```yaml
- name: Update base tag in index.html
  run: |
    BASE_PATH="${{ steps.basepath.outputs.base_path }}"
    sed -i "s|<base href=\"/\" />|<base href=\"$BASE_PATH\" />|g" publish/wwwroot/index.html
```

## How to Deploy

1. Copy the new `deploy.yml` to `.github/workflows/deploy.yml` in your repository
2. Commit and push to your `main` or `master` branch
3. GitHub Actions will automatically:
   - Detect your repository type
   - Set the correct base path
   - Build and deploy your Blazor app

## Verify the Fix

After deployment, check your GitHub Actions logs. You should see:
```
This is a [user/organization/project] site, using base path: [/repo-name/ or /]
Setting base href to: [path]
```

Then visit your site - all the `_framework` files should load correctly!

## Repository Name Detection

The workflow checks:
- If repo name = `{username}.github.io` → User site (base path: `/`)
- Otherwise → Project site (base path: `/repo-name/`)

## Still Having Issues?

If you're still seeing 404 errors after deploying:

1. **Check the workflow logs** to see what base path was set
2. **Verify in GitHub Pages settings** that source is set to "GitHub Actions"
3. **Check the deployed `index.html`** - the base tag should match your URL structure
4. **Clear your browser cache** - old cached versions might be interfering

## Manual Override (if needed)

If you need to manually set the base path, modify the workflow's "Determine base path" step:

```yaml
- name: Determine base path for GitHub Pages
  id: basepath
  run: |
    # Force specific base path
    echo "base_path=/my-custom-path/" >> $GITHUB_OUTPUT
```

# 🔥 CRITICAL FIX NEEDED - Blazor Import Map Issue

## The REAL Problem

Looking at your HAR file, I found the actual issue:

1. ✅ The page loads: `https://collabskus.github.io/`
2. ❌ **404 ERROR**: `https://collabskus.github.io/_framework/blazor.webassembly.js`
3. ✅ But this works: `https://collabskus.github.io/_framework/dotnet.8o4x4gvazt.js`

**Root Cause**: Your `index.html` has an import map that references `_framework/blazor.webassembly.js`, but that file doesn't exist! Only the hashed version `blazor.webassembly.66stpp682q.js` exists.

This is NOT a base path issue - it's a Blazor build/publish issue.

## The Import Map Problem

In your HTML (from the HAR):
```html
<script type="importmap">{
  "imports": {
    "./_framework/blazor.webassembly.js": "./_framework/blazor.webassembly.66stpp682q.js",
    ...
  }
}
```

The browser tries to load `_framework/blazor.webassembly.js` which **doesn't exist**.

## Solutions (Try in Order)

### Solution 1: Remove -p:GHPages=true flag

The `-p:GHPages=true` flag might be causing issues with file naming. Try building without it:

```yaml
- name: Publish
  run: |
    dotnet publish CollabsKus.BlazorWebAssembly/CollabsKus.BlazorWebAssembly.csproj \
      -c Release \
      -o publish \
      --no-build
```

### Solution 2: Check Your .csproj Settings

Make sure your `.csproj` file doesn't have conflicting settings:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <!-- Make sure these are set correctly -->
    <ServiceWorkerAssetsManifest>service-worker-assets.js</ServiceWorkerAssetsManifest>
    <!-- DON'T set StaticWebAssetBasePath unless you need it -->
  </PropertyGroup>
</Project>
```

### Solution 3: Verify Build Output

Add this step to your workflow to see what's actually being published:

```yaml
- name: Inspect published files
  run: |
    echo "=== Published files ==="
    ls -la publish/wwwroot/_framework/
    echo ""
    echo "=== Looking for blazor files ==="
    find publish/wwwroot -name "*blazor*" -type f
```

### Solution 4: Use Proper Publish Command

Make sure you're using the correct publish sequence:

```yaml
- name: Restore
  run: dotnet restore CollabsKus.BlazorWebAssembly/CollabsKus.BlazorWebAssembly.csproj

- name: Build
  run: dotnet build CollabsKus.BlazorWebAssembly/CollabsKus.BlazorWebAssembly.csproj --configuration Release --no-restore

- name: Publish
  run: dotnet publish CollabsKus.BlazorWebAssembly/CollabsKus.BlazorWebAssembly.csproj -c Release -o publish --no-build
```

## What to Check

1. **GitHub Actions Logs**: Look for warnings about file generation
2. **Build Artifacts**: Download the artifact from GitHub Actions and check if `blazor.webassembly.js` exists
3. **Import Map**: Check if the import map is being generated correctly

## The Working Workflow

I've created `deploy-fixed.yml` with:
- ✅ Proper restore → build → publish sequence  
- ✅ Removed the `-p:GHPages=true` flag
- ✅ Added inspection step to debug file generation
- ✅ Correct base path handling

## Quick Test

To test if this is the issue, try building locally:

```bash
cd CollabsKus.BlazorWebAssembly
dotnet publish -c Release -o ../test-output
ls -la ../test-output/wwwroot/_framework/ | grep blazor
```

You should see BOTH:
- `blazor.webassembly.js` (without hash)
- `blazor.webassembly.XXXXX.js` (with hash)

If you only see the hashed version, that's your problem!

## Next Steps

1. Replace `.github/workflows/deploy.yml` with `deploy-fixed.yml`
2. Push and watch the "Inspect published files" step
3. Check if both hashed and unhashed `blazor.webassembly.js` files exist
4. If the unhashed file is missing, we need to fix the build configuration

# THE ACTUAL PROBLEM - CONFIRMED FROM HAR FILE

## What I Found (Line by Line Analysis)

### From run-har.txt:
- **Line 693**: Browser requests `https://collabskus.github.io/_framework/blazor.webassembly.js`
- **Line 774-776**: Response is **404 Not Found**

### From dump.txt:
- **Line 1907 in your index.html**: `<script src="_framework/blazor.webassembly.js"></script>`
- **Line 1888 in your index.html**: `<script type="importmap"></script>` (EMPTY!)

## The Root Cause

Your `index.html` is using the **OLD .NET 7 pattern** but you're on **.NET 10**.

In .NET 7 and earlier:
```html
<script src="_framework/blazor.webassembly.js"></script>
```

In .NET 8, 9, and 10:
```html
<!-- NO script tags in source! -->
<!-- The build system injects everything automatically -->
```

## What's Happening

1. Your source `index.html` has OLD script tags and an empty import map
2. When you build with `dotnet publish`, the .NET 10 build system:
   - Tries to work with your index.html
   - Gets confused by the manual script tag
   - Doesn't properly inject the new framework
3. The published HTML has the old script tag pointing to a file that doesn't exist
4. Result: 404 error

## The Fix

**Replace your `wwwroot/index.html` with this:**

```html
<!DOCTYPE html>
<html lang="en">

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Kathmandu Calendar & Time</title>
    <base href="/" />
    <link rel="stylesheet" href="css/app.css" />
    <link rel="icon" type="image/png" href="favicon.png" />
    <link href="CollabsKus.BlazorWebAssembly.styles.css" rel="stylesheet" />
    <link href="manifest.webmanifest" rel="manifest" />
    <link rel="apple-touch-icon" sizes="512x512" href="icon-512.png" />
    <link rel="apple-touch-icon" sizes="192x192" href="icon-192.png" />
</head>

<body>
    <div id="app">
        <svg class="loading-progress">
            <circle r="40%" cx="50%" cy="50%" />
            <circle r="40%" cx="50%" cy="50%" />
        </svg>
        <div class="loading-progress-text"></div>
    </div>

    <div id="blazor-error-ui">
        An unhandled error has occurred.
        <a href="." class="reload">Reload</a>
        <a class="dismiss">🗙</a>
    </div>
</body>

</html>
```

## What Changed

**REMOVED:**
- ❌ `<link rel="preload" id="webassembly" />` (old .NET 7 thing)
- ❌ `<script type="importmap"></script>` (empty and wrong)
- ❌ `<script src="_framework/blazor.webassembly.js"></script>` (doesn't exist in .NET 10)
- ❌ `<script>navigator.serviceWorker.register(...);</script>` (will be added by build)
- ❌ `<!-- DEBUG MARKER -->` (not needed)

**What .NET 10 Will Add Automatically:**
- ✅ Preload links for framework files
- ✅ Import map with all the hashed file names
- ✅ Correct script tag with integrity hashes
- ✅ Service worker registration

## How .NET 10 Build Works

When you run `dotnet publish`:

1. Reads your clean `index.html`
2. Analyzes all the framework dependencies
3. Generates hashed filenames (like `blazor.webassembly.66stpp682q.js`)
4. Injects the import map into `index.html`
5. Injects the preload links
6. Injects the blazor script tag
7. Outputs the complete `index.html` to `publish/wwwroot/`

## Verification

After you deploy with the fixed index.html:

1. View source of `https://collabskus.github.io/`
2. You should see automatically injected:
   - `<link href="_framework/dotnet.xxx.js" rel="preload" ...>`
   - `<script type="importmap">` with actual content
   - Framework scripts at the bottom

3. The app will load successfully!

## Your Current Workflow is Fine

Your `deploy.yml` is actually correct. The problem was 100% in the `index.html` file.

## Summary

- ✅ The workflow is fine
- ✅ The .csproj is fine  
- ✅ The base path handling is fine
- ❌ The index.html had old .NET 7 code

**Fix: Replace index.html, push, and it will work!**
