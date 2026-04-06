using Discord;

namespace ProjectManagement.Discord.Services;

/// <summary>
/// Provides business-logic operations for Jira slash commands.
/// Returns Discord <see cref="Embed"/> objects ready to be sent as bot replies.
/// </summary>
public interface IJiraService
{
    /// <summary>Search Jira issues and return a formatted embed.</summary>
    Task<Embed> SearchIssuesAsync(string projectKey, string? status, string? issueType);

    /// <summary>Fetch a single issue and return a formatted embed.</summary>
    Task<Embed> GetIssueAsync(string issueKey);

    /// <summary>Create a new issue and return a confirmation embed.</summary>
    Task<Embed> CreateIssueAsync(string projectKey, string summary, string issueType, string? description, string? priority);

    /// <summary>Add a comment to an issue and return a confirmation embed.</summary>
    Task<Embed> AddCommentAsync(string issueKey, string comment);

    /// <summary>Transition an issue and return a confirmation embed.</summary>
    Task<Embed> TransitionIssueAsync(string issueKey, string transitionName);

    /// <summary>List all accessible Jira projects and return a formatted embed.</summary>
    Task<Embed> GetProjectsAsync();
}
