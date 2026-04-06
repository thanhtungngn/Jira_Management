using Discord;
using ProjectManagement.Core.GitHub.Models;

namespace ProjectManagement.Discord.Formatting;

/// <summary>
/// Converts GitHub domain objects into Discord <see cref="Embed"/> instances.
/// All methods are static pure functions to keep them easy to unit-test.
/// </summary>
public static class GitHubEmbedBuilder
{
    /// <summary>
    /// Builds an embed listing GitHub repositories.
    /// </summary>
    /// <param name="repos">The list of repositories to display.</param>
    public static Embed BuildRepoList(List<GitHubRepository> repos)
    {
        var builder = new EmbedBuilder()
            .WithTitle("📦 GitHub Repositories")
            .WithColor(EmbedColors.GitHub)
            .WithFooter($"{repos.Count} repository(ies)");

        if (repos.Count == 0)
        {
            builder.WithDescription("No repositories found.");
            return builder.Build();
        }

        // Limit to 10 entries to stay within Discord's embed field limit.
        foreach (var r in repos.Take(10))
        {
            var visibility = r.Private ? "🔒 Private" : "🌐 Public";
            var desc       = string.IsNullOrWhiteSpace(r.Description) ? "—" : Truncate(r.Description, 80);
            builder.AddField(r.FullName, $"{visibility} | Branch: `{r.DefaultBranch}` | {desc}", inline: false);
        }

        return builder.Build();
    }

    /// <summary>
    /// Builds a detailed embed for a single <see cref="GitHubRepository"/>.
    /// </summary>
    /// <param name="repo">The repository to display.</param>
    public static Embed BuildRepoDetail(GitHubRepository repo)
    {
        var builder = new EmbedBuilder()
            .WithTitle(repo.FullName)
            .WithUrl(repo.HtmlUrl)
            .WithColor(EmbedColors.GitHub)
            .AddField("Visibility",      repo.Private ? "🔒 Private" : "🌐 Public", inline: true)
            .AddField("Default Branch",  $"`{repo.DefaultBranch}`",                  inline: true);

        if (!string.IsNullOrWhiteSpace(repo.Description))
            builder.WithDescription(Truncate(repo.Description, 300));

        if (repo.CreatedAt.HasValue)
            builder.WithTimestamp(repo.CreatedAt.Value);

        return builder.Build();
    }

    /// <summary>
    /// Builds an embed listing GitHub issues.
    /// </summary>
    /// <param name="issues">Issues to display.</param>
    /// <param name="owner">Repository owner.</param>
    /// <param name="repo">Repository name.</param>
    /// <param name="state">State filter used in the query.</param>
    public static Embed BuildIssueList(List<GitHubIssue> issues, string owner, string repo, string state)
    {
        var builder = new EmbedBuilder()
            .WithTitle($"🐛 GitHub Issues — {owner}/{repo} [{state}]")
            .WithColor(EmbedColors.GitHub)
            .WithFooter($"{issues.Count} issue(s)");

        if (issues.Count == 0)
        {
            builder.WithDescription($"No {state} issues found.");
            return builder.Build();
        }

        foreach (var issue in issues.Take(10))
        {
            var author = issue.User?.Login ?? "unknown";
            builder.AddField(
                $"#{issue.Number} — {Truncate(issue.Title, 90)}",
                $"**State:** {issue.State}  **Author:** {author}",
                inline: false);
        }

        return builder.Build();
    }

    /// <summary>
    /// Builds a detailed embed for a single <see cref="GitHubIssue"/>.
    /// </summary>
    /// <param name="issue">The issue to display.</param>
    public static Embed BuildIssueDetail(GitHubIssue issue)
    {
        var builder = new EmbedBuilder()
            .WithTitle($"#{issue.Number} — {Truncate(issue.Title, 200)}")
            .WithUrl(issue.HtmlUrl)
            .WithColor(EmbedColors.GitHub)
            .AddField("State",  issue.State,                     inline: true)
            .AddField("Author", issue.User?.Login ?? "unknown",  inline: true);

        if (!string.IsNullOrWhiteSpace(issue.Body))
            builder.WithDescription(Truncate(issue.Body, 500));

        if (issue.CreatedAt.HasValue)
            builder.WithTimestamp(issue.CreatedAt.Value);

        return builder.Build();
    }

    /// <summary>
    /// Builds a generic error embed with a red colour.
    /// </summary>
    /// <param name="title">Short error title.</param>
    /// <param name="message">The human-readable error message.</param>
    public static Embed BuildError(string title, string message)
    {
        return new EmbedBuilder()
            .WithTitle($"❌ {title}")
            .WithColor(EmbedColors.Error)
            .WithDescription(Truncate(message, 900))
            .Build();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Truncates a string to <paramref name="maxLength"/> characters, appending "…" if truncated.</summary>
    public static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Length <= maxLength ? text : text[..maxLength] + "…";
    }
}
