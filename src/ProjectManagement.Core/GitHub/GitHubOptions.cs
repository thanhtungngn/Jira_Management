namespace ProjectManagement.Core.GitHub;

/// <summary>Configuration settings for the GitHub API client.</summary>
public class GitHubOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "GitHub";

    /// <summary>GitHub personal access token (classic or fine-grained).</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>User-Agent header value sent with every request.</summary>
    public string UserAgent { get; set; } = "ProjectManagement/1.0";
}
