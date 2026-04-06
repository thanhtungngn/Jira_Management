using System.Net;
using System.Net.Http.Json;
using Moq;
using ProjectManagement.Core.GitHub;
using ProjectManagement.Core.GitHub.Models;

namespace ProjectManagement.Api.Tests.GitHub;

public class GitHubControllerTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _http;

    public GitHubControllerTests(ApiTestFactory factory)
    {
        _factory = factory;
        _http    = factory.CreateClient();
    }

    // ── GET /api/repositories ─────────────────────────────────────────────────

    [Fact]
    public async Task GetRepositories_Returns200_WithRepoList()
    {
        _factory.GitHubMock.Setup(c => c.ListRepositoriesAsync())
            .ReturnsAsync([new GitHubRepository { Name = "my-repo", FullName = "owner/my-repo" }]);

        var response = await _http.GetAsync("/api/repositories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var repos = await response.Content.ReadFromJsonAsync<List<GitHubRepository>>();
        Assert.Single(repos!);
        Assert.Equal("owner/my-repo", repos![0].FullName);
    }

    [Fact]
    public async Task GetRepositories_Returns502_WhenClientThrows()
    {
        _factory.GitHubMock.Setup(c => c.ListRepositoriesAsync())
            .ThrowsAsync(new HttpRequestException("Unauthorized"));

        var response = await _http.GetAsync("/api/repositories");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    // ── GET /api/repositories/{owner}/{repo} ──────────────────────────────────

    [Fact]
    public async Task GetRepository_Returns200_WithDetails()
    {
        _factory.GitHubMock.Setup(c => c.GetRepositoryAsync("owner", "my-repo"))
            .ReturnsAsync(new GitHubRepository { Name = "my-repo", FullName = "owner/my-repo", DefaultBranch = "main" });

        var response = await _http.GetAsync("/api/repositories/owner/my-repo");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var repo = await response.Content.ReadFromJsonAsync<GitHubRepository>();
        Assert.Equal("owner/my-repo", repo!.FullName);
        Assert.Equal("main", repo.DefaultBranch);
    }

    [Fact]
    public async Task GetRepository_Returns502_WhenClientThrows()
    {
        _factory.GitHubMock.Setup(c => c.GetRepositoryAsync("owner", "bad-repo"))
            .ThrowsAsync(new HttpRequestException("Not Found"));

        var response = await _http.GetAsync("/api/repositories/owner/bad-repo");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    // ── GET /api/repositories/{owner}/{repo}/branches ─────────────────────────

    [Fact]
    public async Task GetBranches_Returns200_WithBranchList()
    {
        _factory.GitHubMock.Setup(c => c.ListBranchesAsync("owner", "my-repo"))
            .ReturnsAsync([
                new GitHubBranch { Name = "main",    Commit = new GitHubCommitRef { Sha = "abc123" } },
                new GitHubBranch { Name = "develop", Commit = new GitHubCommitRef { Sha = "def456" } },
            ]);

        var response = await _http.GetAsync("/api/repositories/owner/my-repo/branches");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var branches = await response.Content.ReadFromJsonAsync<List<GitHubBranch>>();
        Assert.Equal(2, branches!.Count);
        Assert.Equal("main", branches[0].Name);
    }

    // ── GET /api/repositories/{owner}/{repo}/commits ──────────────────────────

    [Fact]
    public async Task GetCommits_Returns200_WithCommitList()
    {
        _factory.GitHubMock
            .Setup(c => c.ListCommitsAsync(It.Is<ListCommitsRequest>(r =>
                r.Owner == "owner" && r.Repo == "my-repo")))
            .ReturnsAsync([
                new GitHubCommit
                {
                    Sha    = "abc123",
                    Commit = new GitHubCommitDetails { Message = "Initial commit" },
                },
            ]);

        var response = await _http.GetAsync("/api/repositories/owner/my-repo/commits");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var commits = await response.Content.ReadFromJsonAsync<List<GitHubCommit>>();
        Assert.Single(commits!);
        Assert.Equal("abc123", commits![0].Sha);
        Assert.Equal("Initial commit", commits[0].Commit.Message);
    }

    [Fact]
    public async Task GetCommits_PassesBranchFilter_WhenProvided()
    {
        _factory.GitHubMock
            .Setup(c => c.ListCommitsAsync(It.Is<ListCommitsRequest>(r =>
                r.Branch == "feature/xyz")))
            .ReturnsAsync([]);

        var response = await _http.GetAsync("/api/repositories/owner/my-repo/commits?branch=feature%2Fxyz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _factory.GitHubMock.Verify(c => c.ListCommitsAsync(
            It.Is<ListCommitsRequest>(r => r.Branch == "feature/xyz")), Times.Once);
    }

    // ── GET /api/repositories/{owner}/{repo}/issues ───────────────────────────

    [Fact]
    public async Task GetIssues_Returns200_WithIssueList()
    {
        _factory.GitHubMock.Setup(c => c.ListIssuesAsync("owner", "my-repo", "open"))
            .ReturnsAsync([new GitHubIssue { Number = 1, Title = "Bug report", State = "open" }]);

        var response = await _http.GetAsync("/api/repositories/owner/my-repo/issues");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var issues = await response.Content.ReadFromJsonAsync<List<GitHubIssue>>();
        Assert.Single(issues!);
        Assert.Equal("Bug report", issues![0].Title);
    }

    [Fact]
    public async Task GetIssues_PassesStateFilter()
    {
        _factory.GitHubMock.Setup(c => c.ListIssuesAsync("owner", "my-repo", "closed"))
            .ReturnsAsync([]);

        var response = await _http.GetAsync("/api/repositories/owner/my-repo/issues?state=closed");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _factory.GitHubMock.Verify(c => c.ListIssuesAsync("owner", "my-repo", "closed"), Times.Once);
    }

    // ── GET /api/repositories/{owner}/{repo}/issues/{number} ─────────────────

    [Fact]
    public async Task GetIssue_Returns200_WithIssueDetails()
    {
        _factory.GitHubMock.Setup(c => c.GetIssueAsync("owner", "my-repo", 42))
            .ReturnsAsync(new GitHubIssue { Number = 42, Title = "Critical bug", State = "open" });

        var response = await _http.GetAsync("/api/repositories/owner/my-repo/issues/42");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var issue = await response.Content.ReadFromJsonAsync<GitHubIssue>();
        Assert.Equal(42, issue!.Number);
        Assert.Equal("Critical bug", issue.Title);
    }

    [Fact]
    public async Task GetIssue_Returns502_WhenClientThrows()
    {
        _factory.GitHubMock.Setup(c => c.GetIssueAsync("owner", "my-repo", 999))
            .ThrowsAsync(new HttpRequestException("Not Found"));

        var response = await _http.GetAsync("/api/repositories/owner/my-repo/issues/999");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    // ── POST /api/repositories/{owner}/{repo}/issues ──────────────────────────

    [Fact]
    public async Task CreateIssue_Returns201_WithNewIssue()
    {
        var created = new GitHubIssue { Number = 2, Title = "Feature request", State = "open" };
        _factory.GitHubMock
            .Setup(c => c.CreateIssueAsync("owner", "my-repo",
                It.Is<CreateIssueRequest>(r => r.Title == "Feature request")))
            .ReturnsAsync(created);

        var body = new CreateIssueRequest { Title = "Feature request" };
        var response = await _http.PostAsJsonAsync("/api/repositories/owner/my-repo/issues", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var issue = await response.Content.ReadFromJsonAsync<GitHubIssue>();
        Assert.Equal(2, issue!.Number);
        Assert.Equal("Feature request", issue.Title);
    }

    [Fact]
    public async Task CreateIssue_Returns502_WhenClientThrows()
    {
        _factory.GitHubMock
            .Setup(c => c.CreateIssueAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CreateIssueRequest>()))
            .ThrowsAsync(new HttpRequestException("Unprocessable Entity"));

        var body = new CreateIssueRequest { Title = "Test" };
        var response = await _http.PostAsJsonAsync("/api/repositories/owner/my-repo/issues", body);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }
}
