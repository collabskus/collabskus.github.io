namespace CollabsKus.BlazorWebAssembly
{
    public static class PortfolioConfig
    {
        // Personal Information
        public const string FullName = "Kushal";
        public const string Title = "Full Stack Developer | System Administrator | Problem Solver";
        public const string ShortTitle = "Software Developer";

        // Contact Information
        public const string Email = "collabskus@gmail.com";
        public const string Phone = "+1 (555) 123-4567";
        public const string Location = "Denver, Colorado";

        // Social Links
        public const string GitHubUsername = "johndoe";
        public const string LinkedInUsername = "johndoe";
        public const string TwitterUsername = "johndoe";

        // URLs (computed from usernames)
        public static string GitHubUrl => $"https://github.com/{GitHubUsername}";
        public static string LinkedInUrl => $"https://linkedin.com/in/{LinkedInUsername}";
        public static string TwitterUrl => $"https://twitter.com/{TwitterUsername}";

        // Site Configuration
        public const string SiteUrl = "https://johndoe.github.io";
        public const string SiteTitle = "Portfolio";

        // Bio/About
        public const string ShortBio = "I'm a passionate software developer with expertise in multiple programming languages including C#/.NET, Rust, PowerShell, and JavaScript/TypeScript.";

        // You can also load from appsettings.json if you prefer
        // or environment variables for different deployment environments
    }

    // Alternative: Use a singleton service for dependency injection
    public class PortfolioSettings
    {
        public string FullName { get; set; } = "John Doe";
        public string Title { get; set; } = "Full Stack Developer";
        public string Email { get; set; } = "john.doe@example.com";
        public SocialLinks Social { get; set; } = new();

        public class SocialLinks
        {
            public string GitHub { get; set; } = "johndoe";
            public string LinkedIn { get; set; } = "johndoe";
            public string Twitter { get; set; } = "johndoe";
        }
    }
}