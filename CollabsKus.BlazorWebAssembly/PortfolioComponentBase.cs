using Microsoft.AspNetCore.Components;

namespace CollabsKus.BlazorWebAssembly.Components
{
    /// <summary>
    /// Base component that provides portfolio configuration to all pages
    /// </summary>
    public abstract class PortfolioComponentBase : ComponentBase
    {
        [Inject]
        protected PortfolioSettings Settings { get; set; } = default!;

        // Convenience properties for common values
        protected string FullName => Settings.FullName;
        protected string Email => Settings.Email;
        protected string GitHubUrl => $"https://github.com/{Settings.Social.GitHub}";
        protected string LinkedInUrl => $"https://linkedin.com/in/{Settings.Social.LinkedIn}";

        // Helper method for page titles
        protected string PageTitle(string pageName) => $"{pageName} - {FullName}";
    }
}
