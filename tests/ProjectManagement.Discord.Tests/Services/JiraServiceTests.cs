using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProjectManagement.Core.Jira;
using ProjectManagement.Core.Jira.Models;
using ProjectManagement.Discord.Services;

namespace ProjectManagement.Discord.Tests.Services;

/// <summary>
/// Unit tests for <see cref="JiraService"/>.
/// Uses Moq to mock <see cref="IJiraClient"/> so no real network calls are made.
/// </summary>
public class JiraServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (JiraService service, Mock<IJiraClient> clientMock) CreateService()
    {
        var mock    = new Mock<IJiraClient>();
        var service = new JiraService(mock.Object, NullLogger<JiraService>.Instance);
        return (service, mock);
    }

    // ── SearchIssuesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task SearchIssuesAsync_WithResults_ReturnsListEmbed()
    {
        var (svc, mock) = CreateService();
        var result = new SearchResult
        {
            Total  = 2,
            Issues =
            [
                new JiraIssue { Key = "PROJ-1", Fields = new IssueFields { Summary = "Bug one" } },
                new JiraIssue { Key = "PROJ-2", Fields = new IssueFields { Summary = "Task two" } },
            ],
        };
        mock.Setup(c => c.SearchIssuesAsync(It.IsAny<SearchIssuesRequest>()))
            .ReturnsAsync(result);

        var embed = await svc.SearchIssuesAsync("PROJ", null, null);

        Assert.NotNull(embed);
        Assert.Contains("PROJ", embed.Title);
        Assert.Equal(2, embed.Fields.Length);
    }

    [Fact]
    public async Task SearchIssuesAsync_NoResults_ReturnsEmptyDescription()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.SearchIssuesAsync(It.IsAny<SearchIssuesRequest>()))
            .ReturnsAsync(new SearchResult { Total = 0, Issues = [] });

        var embed = await svc.SearchIssuesAsync("PROJ", null, null);

        Assert.NotNull(embed);
        Assert.Contains("No issues", embed.Description ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchIssuesAsync_OnException_ReturnsErrorEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.SearchIssuesAsync(It.IsAny<SearchIssuesRequest>()))
            .ThrowsAsync(new InvalidOperationException("network error"));

        var embed = await svc.SearchIssuesAsync("PROJ", null, null);

        Assert.NotNull(embed);
        Assert.Contains("Error", embed.Title, StringComparison.OrdinalIgnoreCase);
    }

    // ── GetIssueAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetIssueAsync_ValidKey_ReturnsDetailEmbed()
    {
        var (svc, mock) = CreateService();
        var issue = new JiraIssue
        {
            Key    = "PROJ-42",
            Fields = new IssueFields
            {
                Summary  = "Fix login bug",
                Status   = new NamedField { Name = "In Progress" },
                IssueType = new NamedField { Name = "Bug" },
            },
        };
        mock.Setup(c => c.GetIssueAsync("PROJ-42")).ReturnsAsync(issue);

        var embed = await svc.GetIssueAsync("PROJ-42");

        Assert.NotNull(embed);
        Assert.Contains("PROJ-42", embed.Title);
    }

    [Fact]
    public async Task GetIssueAsync_OnException_ReturnsErrorEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.GetIssueAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("not found"));

        var embed = await svc.GetIssueAsync("PROJ-999");

        Assert.NotNull(embed);
        Assert.Contains("Error", embed.Title, StringComparison.OrdinalIgnoreCase);
    }

    // ── CreateIssueAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateIssueAsync_Success_ReturnsCreatedEmbed()
    {
        var (svc, mock) = CreateService();
        var created = new JiraIssue
        {
            Key    = "PROJ-10",
            Fields = new IssueFields
            {
                Summary   = "New task",
                IssueType = new NamedField { Name = "Task" },
                Status    = new NamedField { Name = "To Do" },
            },
        };
        mock.Setup(c => c.CreateIssueAsync(It.IsAny<CreateIssueRequest>()))
            .ReturnsAsync(created);

        var embed = await svc.CreateIssueAsync("PROJ", "New task", "Task", null, null);

        Assert.NotNull(embed);
        Assert.Contains("Created", embed.Title, StringComparison.OrdinalIgnoreCase);
        Assert.True(embed.Fields.Any(f => f.Value.Contains("PROJ-10")));
    }

    [Fact]
    public async Task CreateIssueAsync_OnException_ReturnsErrorEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.CreateIssueAsync(It.IsAny<CreateIssueRequest>()))
            .ThrowsAsync(new InvalidOperationException("bad request"));

        var embed = await svc.CreateIssueAsync("PROJ", "summary", "Task", null, null);

        Assert.NotNull(embed);
        Assert.Contains("Error", embed.Title, StringComparison.OrdinalIgnoreCase);
    }

    // ── AddCommentAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task AddCommentAsync_Success_ReturnsConfirmationEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.AddCommentAsync("PROJ-5", "hello")).Returns(Task.CompletedTask);

        var embed = await svc.AddCommentAsync("PROJ-5", "hello");

        Assert.NotNull(embed);
        Assert.Contains("Comment", embed.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PROJ-5", embed.Description ?? string.Empty);
    }

    [Fact]
    public async Task AddCommentAsync_OnException_ReturnsErrorEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("forbidden"));

        var embed = await svc.AddCommentAsync("PROJ-5", "hello");

        Assert.NotNull(embed);
        Assert.Contains("Error", embed.Title, StringComparison.OrdinalIgnoreCase);
    }

    // ── TransitionIssueAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task TransitionIssueAsync_Success_ReturnsTransitionedEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.TransitionIssueAsync("PROJ-7", "Done")).Returns(Task.CompletedTask);

        var embed = await svc.TransitionIssueAsync("PROJ-7", "Done");

        Assert.NotNull(embed);
        Assert.Contains("Transition", embed.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PROJ-7",     embed.Description ?? string.Empty);
        Assert.Contains("Done",       embed.Description ?? string.Empty);
    }

    [Fact]
    public async Task TransitionIssueAsync_OnException_ReturnsErrorEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.TransitionIssueAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("not found"));

        var embed = await svc.TransitionIssueAsync("PROJ-7", "Done");

        Assert.NotNull(embed);
        Assert.Contains("Error", embed.Title, StringComparison.OrdinalIgnoreCase);
    }

    // ── GetProjectsAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetProjectsAsync_WithProjects_ReturnsListEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.GetProjectsAsync())
            .ReturnsAsync([
                new JiraProject { Key = "ALPHA", Name = "Alpha Project" },
                new JiraProject { Key = "BETA",  Name = "Beta Project"  },
            ]);

        var embed = await svc.GetProjectsAsync();

        Assert.NotNull(embed);
        Assert.Contains("Project", embed.Title, StringComparison.OrdinalIgnoreCase);
        Assert.True(embed.Fields.Length >= 2);
    }

    [Fact]
    public async Task GetProjectsAsync_EmptyList_ReturnsNoProjectsEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.GetProjectsAsync()).ReturnsAsync([]);

        var embed = await svc.GetProjectsAsync();

        Assert.NotNull(embed);
        Assert.Contains("No projects", embed.Description ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetProjectsAsync_OnException_ReturnsErrorEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.GetProjectsAsync())
            .ThrowsAsync(new Exception("server error"));

        var embed = await svc.GetProjectsAsync();

        Assert.NotNull(embed);
        Assert.Contains("Error", embed.Title, StringComparison.OrdinalIgnoreCase);
    }
}
