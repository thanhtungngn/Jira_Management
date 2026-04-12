namespace ProjectManagement.Core.Confluence;

/// <summary>Configuration settings for the Confluence API client.</summary>
public class ConfluenceOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Confluence";

    /// <summary>Confluence instance base URL (e.g. <c>https://yourcompany.atlassian.net</c>).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Email address of the Confluence user.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Confluence API token for the user.</summary>
    public string ApiToken { get; set; } = string.Empty;
}
