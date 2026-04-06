using Discord;

namespace ProjectManagement.Discord.Services;

/// <summary>
/// Provides business-logic operations for GitHub slash commands.
/// Returns Discord <see cref="Embed"/> objects ready to be sent as bot replies.
/// </summary>
public interface IGitHubService
{
    /// <summary>List repositories for the authenticated user and return a formatted embed.</summary>
    Task<Embed> ListRepositoriesAsync();

    /// <summary>Get a single repository and return a formatted embed.</summary>
    Task<Embed> GetRepositoryAsync(string owner, string repo);

    /// <summary>List issues for a repository and return a formatted embed.</summary>
    Task<Embed> ListIssuesAsync(string owner, string repo, string state);

    /// <summary>Get a single issue and return a formatted embed.</summary>
    Task<Embed> GetIssueAsync(string owner, string repo, int issueNumber);
}
