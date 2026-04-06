using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProjectManagement.Core.GitHub;
using ProjectManagement.Core.GitHub.Models;
using ProjectManagement.Discord.Services;

namespace ProjectManagement.Discord.Tests.Services;

/// <summary>
/// Unit tests for <see cref="GitHubService"/>.
/// Uses Moq to mock <see cref="IGitHubClient"/> so no real network calls are made.
/// </summary>
public class GitHubServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (GitHubService service, Mock<IGitHubClient> clientMock) CreateService()
    {
        var mock    = new Mock<IGitHubClient>();
        var service = new GitHubService(mock.Object, NullLogger<GitHubService>.Instance);
        return (service, mock);
    }

    // ── ListRepositoriesAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ListRepositoriesAsync_WithRepos_ReturnsListEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.ListRepositoriesAsync())
            .ReturnsAsync([
                new GitHubRepository { Id = 1, Name = "repo-a", FullName = "owner/repo-a", HtmlUrl = "https://github.com/owner/repo-a", DefaultBranch = "main" },
                new GitHubRepository { Id = 2, Name = "repo-b", FullName = "owner/repo-b", HtmlUrl = "https://github.com/owner/repo-b", DefaultBranch = "main" },
            ]);

        var embed = await svc.ListRepositoriesAsync();

        Assert.NotNull(embed);
        Assert.Contains("Repositories", embed.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, embed.Fields.Length);
    }

    [Fact]
    public async Task ListRepositoriesAsync_EmptyList_ReturnsEmptyDescription()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.ListRepositoriesAsync()).ReturnsAsync([]);

        var embed = await svc.ListRepositoriesAsync();

        Assert.NotNull(embed);
        Assert.Contains("No repositories", embed.Description ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListRepositoriesAsync_OnException_ReturnsErrorEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.ListRepositoriesAsync()).ThrowsAsync(new Exception("unauthorized"));

        var embed = await svc.ListRepositoriesAsync();

        Assert.NotNull(embed);
        Assert.Contains("Error", embed.Title, StringComparison.OrdinalIgnoreCase);
    }

    // ── GetRepositoryAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetRepositoryAsync_ValidRepo_ReturnsDetailEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.GetRepositoryAsync("owner", "repo"))
            .ReturnsAsync(new GitHubRepository
            {
                FullName      = "owner/repo",
                HtmlUrl       = "https://github.com/owner/repo",
                DefaultBranch = "main",
                Private       = false,
            });

        var embed = await svc.GetRepositoryAsync("owner", "repo");

        Assert.NotNull(embed);
        Assert.Contains("owner/repo", embed.Title);
    }

    [Fact]
    public async Task GetRepositoryAsync_OnException_ReturnsErrorEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.GetRepositoryAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("not found"));

        var embed = await svc.GetRepositoryAsync("owner", "missing");

        Assert.NotNull(embed);
        Assert.Contains("Error", embed.Title, StringComparison.OrdinalIgnoreCase);
    }

    // ── ListIssuesAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task ListIssuesAsync_WithIssues_ReturnsListEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.ListIssuesAsync("owner", "repo", "open"))
            .ReturnsAsync([
                new GitHubIssue { Id = 1, Number = 1, Title = "Bug A", State = "open", HtmlUrl = "https://github.com/owner/repo/issues/1" },
            ]);

        var embed = await svc.ListIssuesAsync("owner", "repo", "open");

        Assert.NotNull(embed);
        Assert.Contains("Issues", embed.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Single(embed.Fields);
    }

    [Fact]
    public async Task ListIssuesAsync_EmptyList_ReturnsNoIssuesDescription()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.ListIssuesAsync("owner", "repo", "open")).ReturnsAsync([]);

        var embed = await svc.ListIssuesAsync("owner", "repo", "open");

        Assert.NotNull(embed);
        Assert.Contains("No", embed.Description ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListIssuesAsync_OnException_ReturnsErrorEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.ListIssuesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("server error"));

        var embed = await svc.ListIssuesAsync("owner", "repo", "open");

        Assert.NotNull(embed);
        Assert.Contains("Error", embed.Title, StringComparison.OrdinalIgnoreCase);
    }

    // ── GetIssueAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetIssueAsync_ValidIssue_ReturnsDetailEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.GetIssueAsync("owner", "repo", 42))
            .ReturnsAsync(new GitHubIssue
            {
                Number  = 42,
                Title   = "Some bug",
                State   = "open",
                HtmlUrl = "https://github.com/owner/repo/issues/42",
            });

        var embed = await svc.GetIssueAsync("owner", "repo", 42);

        Assert.NotNull(embed);
        Assert.Contains("42", embed.Title);
    }

    [Fact]
    public async Task GetIssueAsync_OnException_ReturnsErrorEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.GetIssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ThrowsAsync(new HttpRequestException("not found"));

        var embed = await svc.GetIssueAsync("owner", "repo", 999);

        Assert.NotNull(embed);
        Assert.Contains("Error", embed.Title, StringComparison.OrdinalIgnoreCase);
    }
}
