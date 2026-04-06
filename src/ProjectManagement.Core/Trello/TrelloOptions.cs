namespace ProjectManagement.Core.Trello;

/// <summary>Configuration settings for the Trello API client.</summary>
public class TrelloOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Trello";

    /// <summary>Trello developer API key.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Trello OAuth token for the user.</summary>
    public string Token { get; set; } = string.Empty;
}
