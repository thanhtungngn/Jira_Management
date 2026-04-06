using Discord;
using ProjectManagement.Core.Jira.Models;

namespace ProjectManagement.Discord.Formatting;

/// <summary>
/// Converts Jira domain objects into Discord <see cref="Embed"/> instances.
/// All methods are static pure functions to keep them easy to unit-test.
/// </summary>
public static class JiraEmbedBuilder
{
    /// <summary>
    /// Builds an embed listing Jira issues from a <see cref="SearchResult"/>.
    /// Shows up to 10 issues; truncates if more are returned.
    /// </summary>
    /// <param name="result">The search result from <c>IJiraClient.SearchIssuesAsync</c>.</param>
    /// <param name="projectKey">The Jira project key used in the query.</param>
    public static Embed BuildIssueList(SearchResult result, string projectKey)
    {
        var builder = new EmbedBuilder()
            .WithTitle($"🔍 Jira Issues — {projectKey}")
            .WithColor(EmbedColors.Jira)
            .WithFooter($"Total: {result.Total} issue(s)");

        if (result.Issues.Count == 0)
        {
            builder.WithDescription("No issues found matching the criteria.");
            return builder.Build();
        }

        // Show at most 10 issues to stay within Discord's embed size limits.
        foreach (var issue in result.Issues.Take(10))
        {
            var status   = issue.Fields.Status?.Name ?? "Unknown";
            var type     = issue.Fields.IssueType?.Name ?? "Unknown";
            var assignee = issue.Fields.Assignee?.DisplayName ?? "Unassigned";

            builder.AddField(
                $"{issue.Key} — {Truncate(issue.Fields.Summary, 90)}",
                $"**Status:** {status}  **Type:** {type}  **Assignee:** {assignee}",
                inline: false);
        }

        if (result.Total > 10)
            builder.WithDescription($"Showing first 10 of {result.Total} issues.");

        return builder.Build();
    }

    /// <summary>
    /// Builds a detailed embed for a single <see cref="JiraIssue"/>.
    /// </summary>
    /// <param name="issue">The issue to display.</param>
    public static Embed BuildIssueDetail(JiraIssue issue)
    {
        var f = issue.Fields;

        var builder = new EmbedBuilder()
            .WithTitle($"{issue.Key} — {Truncate(f.Summary, 200)}")
            .WithColor(EmbedColors.Jira)
            .AddField("Status",    Fallback(f.Status?.Name),           inline: true)
            .AddField("Type",      Fallback(f.IssueType?.Name),        inline: true)
            .AddField("Priority",  Fallback(f.Priority?.Name),         inline: true)
            .AddField("Assignee",  Fallback(f.Assignee?.DisplayName, "Unassigned"), inline: true)
            .AddField("Reporter",  Fallback(f.Reporter?.DisplayName),  inline: true);

        // Render the ADF description as plain text (extract leaf text nodes).
        var descText = ExtractAdfText(f.Description);
        if (!string.IsNullOrWhiteSpace(descText))
            builder.AddField("Description", Truncate(descText, 500), inline: false);

        // Show the three most recent comments if available.
        var comments = f.Comment?.Comments ?? [];
        if (comments.Count > 0)
        {
            var commentText = string.Join("\n\n",
                comments.TakeLast(3)
                    .Select(c => $"**{c.Author.DisplayName}** ({c.Created:yyyy-MM-dd}):\n{Truncate(ExtractAdfText(c.Body), 200)}"));

            builder.AddField($"Comments ({f.Comment!.Total} total)", commentText, inline: false);
        }

        if (f.Created.HasValue) builder.WithTimestamp(f.Created.Value);

        return builder.Build();
    }

    /// <summary>
    /// Builds a confirmation embed for a newly created Jira issue.
    /// </summary>
    /// <param name="issue">The created issue.</param>
    public static Embed BuildCreated(JiraIssue issue)
    {
        return new EmbedBuilder()
            .WithTitle("✅ Jira Issue Created")
            .WithColor(EmbedColors.Success)
            .AddField("Key",     issue.Key,               inline: true)
            .AddField("Summary", issue.Fields.Summary,    inline: false)
            .AddField("Type",    issue.Fields.IssueType?.Name ?? "—", inline: true)
            .AddField("Status",  issue.Fields.Status?.Name    ?? "—", inline: true)
            .Build();
    }

    /// <summary>
    /// Builds a confirmation embed for adding a comment.
    /// </summary>
    /// <param name="issueKey">The issue key the comment was added to.</param>
    public static Embed BuildCommentAdded(string issueKey)
    {
        return new EmbedBuilder()
            .WithTitle("💬 Comment Added")
            .WithColor(EmbedColors.Success)
            .WithDescription($"Comment successfully added to **{issueKey}**.")
            .Build();
    }

    /// <summary>
    /// Builds a confirmation embed for a status transition.
    /// </summary>
    /// <param name="issueKey">The issue key that was transitioned.</param>
    /// <param name="transitionName">The name of the transition that was applied.</param>
    public static Embed BuildTransitioned(string issueKey, string transitionName)
    {
        return new EmbedBuilder()
            .WithTitle("🔄 Issue Transitioned")
            .WithColor(EmbedColors.Warning)
            .WithDescription($"**{issueKey}** transitioned to **{transitionName}**.")
            .Build();
    }

    /// <summary>
    /// Builds an embed listing Jira projects.
    /// </summary>
    /// <param name="projects">The list of projects to display.</param>
    public static Embed BuildProjectList(List<JiraProject> projects)
    {
        var builder = new EmbedBuilder()
            .WithTitle("📋 Jira Projects")
            .WithColor(EmbedColors.Jira)
            .WithFooter($"{projects.Count} project(s)");

        if (projects.Count == 0)
        {
            builder.WithDescription("No projects found.");
            return builder.Build();
        }

        foreach (var p in projects.Take(15))
            builder.AddField(p.Key, Truncate(p.Name, 100), inline: true);

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

    // ── Internal helpers ──────────────────────────────────────────────────────

    /// <summary>Truncates a string to <paramref name="maxLength"/> characters, appending "…" if truncated.</summary>
    public static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Length <= maxLength ? text : text[..maxLength] + "…";
    }

    /// <summary>Returns <paramref name="value"/> when non-empty, otherwise returns <paramref name="fallback"/>.</summary>
    private static string Fallback(string? value, string fallback = "—")
        => string.IsNullOrWhiteSpace(value) ? fallback : value;

    /// <summary>
    /// Recursively extracts plain text from an Atlassian Document Format (ADF) node tree.
    /// Returns an empty string when <paramref name="node"/> is <c>null</c>.
    /// </summary>
    public static string ExtractAdfText(AdfDocument? node)
    {
        if (node is null) return string.Empty;
        return ExtractNodeText(node.Content);
    }

    private static string ExtractNodeText(List<AdfNode>? nodes)
    {
        if (nodes is null) return string.Empty;

        var parts = new List<string>();
        foreach (var n in nodes)
        {
            if (!string.IsNullOrEmpty(n.Text))
                parts.Add(n.Text);

            if (n.Content?.Count > 0)
                parts.Add(ExtractNodeText(n.Content));
        }
        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}
