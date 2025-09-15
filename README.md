# CollabsKus.BlazorWebAssembly

A personal portfolio website built with Blazor WebAssembly and .NET 10, featuring automatic deployment to GitHub Pages.

## 🚧 Work in Progress

This repository is currently under active development. Features and documentation may be incomplete or subject to change.

## 🤖 AI-Assisted Development

This project includes code generated and assisted by large language models (LLMs) such as Claude. While all code has been reviewed and tested, please be aware that some portions were created with AI assistance.

## Features

- **Modern Blazor WASM**: Built with .NET 10.0 RC and the latest Blazor WebAssembly features
- **Progressive Web App**: Includes service worker for offline functionality
- **Responsive Design**: Mobile-first responsive layout
- **Automatic Deployment**: GitHub Actions workflow for continuous deployment to GitHub Pages
- **Performance Optimized**: Efficient bundle size and loading times

## Tech Stack

- .NET 10.0 (Release Candidate)
- Blazor WebAssembly
- C# with nullable reference types enabled
- Service Worker for PWA functionality
- GitHub Actions for CI/CD

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later
- Modern web browser with WebAssembly support

### Local Development

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd CollabsKus.BlazorWebAssembly
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

4. Open your browser and navigate to `https://localhost:5001` (or the URL shown in the console)

### Building for Production

```bash
dotnet publish -c Release -o ./publish
```

## Deployment

The project is configured for automatic deployment to GitHub Pages using GitHub Actions. The workflow:

1. Triggers on pushes to `main` or `master` branches
2. Builds the project using .NET 10.0
3. Publishes the Blazor WASM output
4. Deploys to GitHub Pages

### Manual Deployment Setup

1. Enable GitHub Pages in your repository settings
2. Set the source to "GitHub Actions"
3. Push to your main branch to trigger the deployment

## Configuration

The portfolio settings can be configured through the `PortfolioSettings` class and dependency injection. Update your personal information, social links, and other details in the configuration.

## Project Structure

- `Pages/` - Blazor pages and components
- `wwwroot/` - Static assets and service worker
- `CollabsKus.BlazorWebAssembly.csproj` - Project file with package references
- `.github/workflows/` - GitHub Actions deployment workflow

## Contributing

As this is a personal portfolio project, contributions are not expected. However, if you notice any issues or have suggestions, feel free to open an issue.

## License

AGPL

## Contact

collabs kus at gmail dot com
