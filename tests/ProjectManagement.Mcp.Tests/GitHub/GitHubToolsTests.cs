using Moq;
using ProjectManagement.Core.GitHub;
using ProjectManagement.Core.GitHub.Models;
using ProjectManagement.Mcp.GitHub;

namespace ProjectManagement.Mcp.Tests.GitHub;

public class GitHubToolsTests
{
    private readonly Mock<IGitHubClient> _clientMock = new();
    private readonly GitHubTools _tools;

    public GitHubToolsTests()
    {
        _tools = new GitHubTools(_clientMock.Object);
    }

    [Fact]
    public async Task ListRepositoriesAsync_DelegatesToClient()
    {
        var expected = new List<GitHubRepository> { new() { Name = "my-repo", FullName = "owner/my-repo" } };
        _clientMock.Setup(c => c.ListRepositoriesAsync()).ReturnsAsync(expected);

        var result = await _tools.ListRepositoriesAsync();

        Assert.Single(result);
        Assert.Equal("my-repo", result[0].Name);
    }

    [Fact]
    public async Task GetRepositoryAsync_DelegatesToClient()
    {
        var expected = new GitHubRepository { Name = "my-repo", FullName = "owner/my-repo" };
        _clientMock.Setup(c => c.GetRepositoryAsync("owner", "my-repo")).ReturnsAsync(expected);

        var result = await _tools.GetRepositoryAsync("owner", "my-repo");

        Assert.Equal("owner/my-repo", result.FullName);
    }

    [Fact]
    public async Task ListBranchesAsync_DelegatesToClient()
    {
        var expected = new List<GitHubBranch>
        {
            new() { Name = "main", Commit = new GitHubCommitRef { Sha = "abc123" } },
        };
        _clientMock.Setup(c => c.ListBranchesAsync("owner", "my-repo")).ReturnsAsync(expected);

        var result = await _tools.ListBranchesAsync("owner", "my-repo");

        Assert.Single(result);
        Assert.Equal("main", result[0].Name);
    }

    [Fact]
    public async Task ListCommitsAsync_DelegatesToClient()
    {
        var expected = new List<GitHubCommit>
        {
            new() { Sha = "abc123", Commit = new GitHubCommitDetails { Message = "Initial commit" } },
        };
        _clientMock
            .Setup(c => c.ListCommitsAsync(It.Is<ListCommitsRequest>(r =>
                r.Owner == "owner" && r.Repo == "my-repo" && r.Branch == "main")))
            .ReturnsAsync(expected);

        var result = await _tools.ListCommitsAsync("owner", "my-repo", branch: "main");

        Assert.Single(result);
        Assert.Equal("abc123", result[0].Sha);
    }

    [Fact]
    public async Task ListIssuesAsync_DelegatesToClient()
    {
        var expected = new List<GitHubIssue> { new() { Number = 1, Title = "Bug" } };
        _clientMock.Setup(c => c.ListIssuesAsync("owner", "my-repo", "open")).ReturnsAsync(expected);

        var result = await _tools.ListIssuesAsync("owner", "my-repo");

        Assert.Single(result);
        Assert.Equal("Bug", result[0].Title);
    }

    [Fact]
    public async Task CreateIssueAsync_DelegatesToClient()
    {
        var expected = new GitHubIssue { Number = 2, Title = "Feature" };
        _clientMock
            .Setup(c => c.CreateIssueAsync("owner", "my-repo",
                It.Is<CreateIssueRequest>(r => r.Title == "Feature")))
            .ReturnsAsync(expected);

        var result = await _tools.CreateIssueAsync("owner", "my-repo", "Feature");

        Assert.Equal(2, result.Number);
    }
}
