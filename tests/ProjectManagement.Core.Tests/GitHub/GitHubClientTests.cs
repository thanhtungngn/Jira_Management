using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using ProjectManagement.Core.GitHub;
using ProjectManagement.Core.GitHub.Models;

namespace ProjectManagement.Core.Tests.GitHub;

public class GitHubClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private static (GitHubClient client, Mock<HttpMessageHandler> handlerMock) CreateClient(
        HttpStatusCode statusCode, object responseBody)
    {
        var json = JsonSerializer.Serialize(responseBody, JsonOptions);
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.github.com/"),
        };

        return (new GitHubClient(httpClient), handlerMock);
    }

    // ── ListRepositoriesAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ListRepositoriesAsync_ReturnsRepositories()
    {
        var payload = new[]
        {
            new { id = 1L, name = "my-repo", full_name = "owner/my-repo", description = "A repo", @private = false, html_url = "https://github.com/owner/my-repo", default_branch = "main" },
        };
        var (client, _) = CreateClient(HttpStatusCode.OK, payload);

        var repos = await client.ListRepositoriesAsync();

        Assert.Single(repos);
        Assert.Equal("my-repo", repos[0].Name);
        Assert.Equal("owner/my-repo", repos[0].FullName);
    }

    [Fact]
    public async Task ListRepositoriesAsync_ThrowsHttpRequestException_OnApiError()
    {
        var (client, _) = CreateClient(HttpStatusCode.Unauthorized, new { message = "Bad credentials" });

        await Assert.ThrowsAsync<HttpRequestException>(() => client.ListRepositoriesAsync());
    }

    // ── GetRepositoryAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetRepositoryAsync_ReturnsRepository()
    {
        var payload = new { id = 1L, name = "my-repo", full_name = "owner/my-repo", description = "A repo", @private = false, html_url = "https://github.com/owner/my-repo", default_branch = "main" };
        var (client, _) = CreateClient(HttpStatusCode.OK, payload);

        var repo = await client.GetRepositoryAsync("owner", "my-repo");

        Assert.Equal("my-repo", repo.Name);
        Assert.Equal("owner/my-repo", repo.FullName);
    }

    // ── ListBranchesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListBranchesAsync_ReturnsBranches()
    {
        var payload = new[]
        {
            new { name = "main",    commit = new { sha = "abc123", url = "" }, @protected = true },
            new { name = "develop", commit = new { sha = "def456", url = "" }, @protected = false },
        };
        var (client, _) = CreateClient(HttpStatusCode.OK, payload);

        var branches = await client.ListBranchesAsync("owner", "my-repo");

        Assert.Equal(2, branches.Count);
        Assert.Equal("main", branches[0].Name);
        Assert.Equal("abc123", branches[0].Commit.Sha);
    }

    // ── ListCommitsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task ListCommitsAsync_ReturnsCommits()
    {
        var payload = new[]
        {
            new
            {
                sha = "abc123",
                html_url = "https://github.com/owner/my-repo/commit/abc123",
                commit = new { message = "Initial commit", author = new { name = "Alice", email = "alice@example.com", date = (DateTime?)null } },
                author = (object?)null,
            },
        };
        var (client, _) = CreateClient(HttpStatusCode.OK, payload);

        var commits = await client.ListCommitsAsync(new ListCommitsRequest { Owner = "owner", Repo = "my-repo" });

        Assert.Single(commits);
        Assert.Equal("abc123", commits[0].Sha);
        Assert.Equal("Initial commit", commits[0].Commit.Message);
    }

    [Fact]
    public async Task ListCommitsAsync_IncludesBranchInUrl_WhenProvided()
    {
        var payload = Array.Empty<object>();
        var (client, handlerMock) = CreateClient(HttpStatusCode.OK, payload);

        await client.ListCommitsAsync(new ListCommitsRequest { Owner = "owner", Repo = "my-repo", Branch = "feature/xyz" });

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.Query.Contains("sha=feature")),
            ItExpr.IsAny<CancellationToken>());
    }

    // ── ListIssuesAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListIssuesAsync_ReturnsIssues()
    {
        var payload = new[]
        {
            new { id = 1L, number = 42, title = "Bug report", body = "Something is wrong", state = "open", html_url = "https://github.com/owner/my-repo/issues/42", user = (object?)null },
        };
        var (client, _) = CreateClient(HttpStatusCode.OK, payload);

        var issues = await client.ListIssuesAsync("owner", "my-repo");

        Assert.Single(issues);
        Assert.Equal(42, issues[0].Number);
        Assert.Equal("Bug report", issues[0].Title);
    }

    // ── CreateIssueAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateIssueAsync_ReturnsCreatedIssue()
    {
        var payload = new { id = 2L, number = 43, title = "Feature request", body = (string?)null, state = "open", html_url = "https://github.com/owner/my-repo/issues/43", user = (object?)null };
        var (client, handlerMock) = CreateClient(HttpStatusCode.Created, payload);

        var issue = await client.CreateIssueAsync("owner", "my-repo", new CreateIssueRequest { Title = "Feature request" });

        Assert.Equal(43, issue.Number);
        Assert.Equal("Feature request", issue.Title);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
            ItExpr.IsAny<CancellationToken>());
    }
}
