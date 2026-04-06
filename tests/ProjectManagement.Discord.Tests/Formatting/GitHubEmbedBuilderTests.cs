using ProjectManagement.Core.GitHub.Models;
using ProjectManagement.Discord.Formatting;

namespace ProjectManagement.Discord.Tests.Formatting;

/// <summary>
/// Unit tests for <see cref="GitHubEmbedBuilder"/>.
/// Pure-function tests — no mocking required.
/// </summary>
public class GitHubEmbedBuilderTests
{
    // ── BuildRepoList ──────────────────────────────────────────────────────────

    [Fact]
    public void BuildRepoList_WithRepos_RendersFields()
    {
        var repos = new List<GitHubRepository>
        {
            new() { Id = 1, Name = "repo-a", FullName = "org/repo-a", HtmlUrl = "https://github.com/org/repo-a", DefaultBranch = "main" },
            new() { Id = 2, Name = "repo-b", FullName = "org/repo-b", HtmlUrl = "https://github.com/org/repo-b", DefaultBranch = "main" },
        };

        var embed = GitHubEmbedBuilder.BuildRepoList(repos);

        Assert.Contains("Repositories", embed.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, embed.Fields.Length);
    }

    [Fact]
    public void BuildRepoList_EmptyList_ShowsNoReposMessage()
    {
        var embed = GitHubEmbedBuilder.BuildRepoList([]);

        Assert.Contains("No repositories", embed.Description ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildRepoList_MoreThanTenRepos_ShowsFirstTen()
    {
        var repos = Enumerable.Range(1, 15)
            .Select(i => new GitHubRepository { Id = i, Name = $"repo-{i}", FullName = $"org/repo-{i}", HtmlUrl = $"https://github.com/org/repo-{i}", DefaultBranch = "main" })
            .ToList();

        var embed = GitHubEmbedBuilder.BuildRepoList(repos);

        Assert.Equal(10, embed.Fields.Length);
    }

    [Fact]
    public void BuildRepoList_PrivateRepo_ShowsLockIcon()
    {
        var repos = new List<GitHubRepository>
        {
            new() { Id = 1, FullName = "org/secret", Private = true, HtmlUrl = "https://github.com/org/secret", DefaultBranch = "main" },
        };

        var embed = GitHubEmbedBuilder.BuildRepoList(repos);

        Assert.Contains("🔒", embed.Fields[0].Value);
    }

    // ── BuildRepoDetail ────────────────────────────────────────────────────────

    [Fact]
    public void BuildRepoDetail_WithDescription_IncludesDescription()
    {
        var repo = new GitHubRepository
        {
            FullName      = "org/described",
            HtmlUrl       = "https://github.com/org/described",
            DefaultBranch = "develop",
            Description   = "A test repository",
            CreatedAt     = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        var embed = GitHubEmbedBuilder.BuildRepoDetail(repo);

        Assert.Equal("org/described", embed.Title);
        Assert.Contains("A test repository", embed.Description ?? "");
    }

    [Fact]
    public void BuildRepoDetail_NoDescription_NoDescriptionSet()
    {
        var repo = new GitHubRepository
        {
            FullName      = "org/nodesc",
            HtmlUrl       = "https://github.com/org/nodesc",
            DefaultBranch = "main",
            Description   = null,
        };

        var embed = GitHubEmbedBuilder.BuildRepoDetail(repo);

        Assert.Null(embed.Description);
    }

    // ── BuildIssueList ─────────────────────────────────────────────────────────

    [Fact]
    public void BuildIssueList_WithIssues_RendersFields()
    {
        var issues = new List<GitHubIssue>
        {
            new() { Id = 1, Number = 1, Title = "Issue A", State = "open", HtmlUrl = "https://github.com/org/r/issues/1" },
        };

        var embed = GitHubEmbedBuilder.BuildIssueList(issues, "org", "repo", "open");

        Assert.Contains("Issues", embed.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Single(embed.Fields);
    }

    [Fact]
    public void BuildIssueList_EmptyList_ShowsNoIssuesMessage()
    {
        var embed = GitHubEmbedBuilder.BuildIssueList([], "org", "repo", "open");

        Assert.Contains("No", embed.Description ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildIssueList_MoreThanTenIssues_ShowsFirstTen()
    {
        var issues = Enumerable.Range(1, 13)
            .Select(i => new GitHubIssue { Id = i, Number = i, Title = $"Issue {i}", State = "open", HtmlUrl = $"https://github.com/o/r/issues/{i}" })
            .ToList();

        var embed = GitHubEmbedBuilder.BuildIssueList(issues, "o", "r", "open");

        Assert.Equal(10, embed.Fields.Length);
    }

    // ── BuildIssueDetail ───────────────────────────────────────────────────────

    [Fact]
    public void BuildIssueDetail_ContainsNumberAndTitle()
    {
        var issue = new GitHubIssue
        {
            Number  = 7,
            Title   = "My issue",
            State   = "open",
            HtmlUrl = "https://github.com/o/r/issues/7",
        };

        var embed = GitHubEmbedBuilder.BuildIssueDetail(issue);

        Assert.Contains("7",         embed.Title);
        Assert.Contains("My issue",  embed.Title);
    }

    [Fact]
    public void BuildIssueDetail_WithBody_IncludesBody()
    {
        var issue = new GitHubIssue
        {
            Number    = 1,
            Title     = "Bug",
            State     = "open",
            HtmlUrl   = "https://github.com/o/r/issues/1",
            Body      = "Steps to reproduce...",
            CreatedAt = new DateTime(2024, 3, 10, 0, 0, 0, DateTimeKind.Utc),
        };

        var embed = GitHubEmbedBuilder.BuildIssueDetail(issue);

        Assert.Contains("Steps to reproduce", embed.Description ?? "");
    }

    // ── BuildError ─────────────────────────────────────────────────────────────

    [Fact]
    public void BuildError_ContainsTitleAndMessage()
    {
        var embed = GitHubEmbedBuilder.BuildError("GitHub Error", "rate limited");

        Assert.Contains("GitHub Error", embed.Title);
        Assert.Contains("rate limited", embed.Description ?? "");
    }

    // ── Truncate ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null,         10, "")]
    [InlineData("",           10, "")]
    [InlineData("short",      10, "short")]
    [InlineData("longstring", 4,  "long…")]
    public void Truncate_HandlesVariousInputs(string? input, int max, string expected)
    {
        Assert.Equal(expected, GitHubEmbedBuilder.Truncate(input, max));
    }
}
