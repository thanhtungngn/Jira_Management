using Moq;
using ProjectManagement.Core.Jira;
using ProjectManagement.Core.Jira.Models;
using ProjectManagement.Mcp.Jira;

namespace ProjectManagement.Mcp.Tests.Jira;

public class JiraToolsTests
{
    private readonly Mock<IJiraClient> _clientMock = new();
    private readonly JiraTools _tools;

    public JiraToolsTests()
    {
        _tools = new JiraTools(_clientMock.Object);
    }

    [Fact]
    public async Task SearchIssuesAsync_DelegatesToClient()
    {
        var expected = new SearchResult { Total = 1, Issues = [new JiraIssue { Key = "PROJ-1" }] };
        _clientMock
            .Setup(c => c.SearchIssuesAsync(It.Is<SearchIssuesRequest>(r =>
                r.ProjectKey == "PROJ" && r.Status == "In Progress")))
            .ReturnsAsync(expected);

        var result = await _tools.SearchIssuesAsync("PROJ", status: "In Progress");

        Assert.Equal(1, result.Total);
        Assert.Equal("PROJ-1", result.Issues[0].Key);
    }

    [Fact]
    public async Task GetIssueAsync_DelegatesToClient()
    {
        var expected = new JiraIssue { Key = "PROJ-1", Fields = new IssueFields { Summary = "Bug" } };
        _clientMock.Setup(c => c.GetIssueAsync("PROJ-1")).ReturnsAsync(expected);

        var result = await _tools.GetIssueAsync("PROJ-1");

        Assert.Equal("PROJ-1", result.Key);
        Assert.Equal("Bug", result.Fields.Summary);
    }

    [Fact]
    public async Task CreateIssueAsync_DelegatesToClient()
    {
        var expected = new JiraIssue { Key = "PROJ-2" };
        _clientMock
            .Setup(c => c.CreateIssueAsync(It.Is<CreateIssueRequest>(r =>
                r.ProjectKey == "PROJ" && r.Summary == "New task")))
            .ReturnsAsync(expected);

        var result = await _tools.CreateIssueAsync("PROJ", "New task");

        Assert.Equal("PROJ-2", result.Key);
    }

    [Fact]
    public async Task TransitionIssueAsync_DelegatesToClient()
    {
        _clientMock.Setup(c => c.TransitionIssueAsync("PROJ-1", "Done")).Returns(Task.CompletedTask);

        await _tools.TransitionIssueAsync("PROJ-1", "Done");

        _clientMock.Verify(c => c.TransitionIssueAsync("PROJ-1", "Done"), Times.Once);
    }

    [Fact]
    public async Task GetProjectsAsync_DelegatesToClient()
    {
        var expected = new List<JiraProject> { new() { Key = "PROJ", Name = "My Project" } };
        _clientMock.Setup(c => c.GetProjectsAsync()).ReturnsAsync(expected);

        var result = await _tools.GetProjectsAsync();

        Assert.Single(result);
        Assert.Equal("PROJ", result[0].Key);
    }
}
