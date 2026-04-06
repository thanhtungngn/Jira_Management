namespace ProjectManagement.Core.Jira;

/// <summary>Configuration settings for the Jira API client.</summary>
public class JiraOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Jira";

    /// <summary>Jira instance base URL (e.g. <c>https://yourcompany.atlassian.net</c>).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Email address of the Jira user.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Jira API token for the user.</summary>
    public string ApiToken { get; set; } = string.Empty;
}
