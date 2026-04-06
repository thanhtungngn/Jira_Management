using System.Net;
using System.Net.Http.Json;
using Moq;
using ProjectManagement.Core.Jira;
using ProjectManagement.Core.Jira.Models;

namespace ProjectManagement.Api.Tests.Jira;

public class JiraControllerTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _http;

    public JiraControllerTests(ApiTestFactory factory)
    {
        _factory = factory;
        _http    = factory.CreateClient();
    }

    // ── GET /api/projects ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetProjects_Returns200_WithProjectList()
    {
        _factory.JiraMock.Setup(c => c.GetProjectsAsync())
            .ReturnsAsync([new JiraProject { Key = "PROJ", Name = "My Project" }]);

        var response = await _http.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var projects = await response.Content.ReadFromJsonAsync<List<JiraProject>>();
        Assert.Single(projects!);
        Assert.Equal("PROJ", projects![0].Key);
    }

    [Fact]
    public async Task GetProjects_Returns502_WhenClientThrows()
    {
        _factory.JiraMock.Setup(c => c.GetProjectsAsync())
            .ThrowsAsync(new HttpRequestException("Unauthorized"));

        var response = await _http.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    // ── GET /api/projects/{key} ───────────────────────────────────────────────

    [Fact]
    public async Task GetProject_Returns200_WithProject()
    {
        _factory.JiraMock.Setup(c => c.GetProjectAsync("PROJ"))
            .ReturnsAsync(new JiraProject { Key = "PROJ", Name = "My Project" });

        var response = await _http.GetAsync("/api/projects/PROJ");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = await response.Content.ReadFromJsonAsync<JiraProject>();
        Assert.Equal("PROJ", project!.Key);
    }

    // ── GET /api/issues ───────────────────────────────────────────────────────

    [Fact]
    public async Task SearchIssues_Returns200_WithResults()
    {
        _factory.JiraMock
            .Setup(c => c.SearchIssuesAsync(It.Is<SearchIssuesRequest>(r => r.ProjectKey == "PROJ")))
            .ReturnsAsync(new SearchResult
            {
                Total = 1,
                Issues = [new JiraIssue { Key = "PROJ-1", Fields = new IssueFields { Summary = "Bug" } }],
            });

        var response = await _http.GetAsync("/api/issues?projectKey=PROJ");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SearchResult>();
        Assert.Equal(1, result!.Total);
        Assert.Equal("PROJ-1", result.Issues[0].Key);
    }

    // ── GET /api/issues/{key} ─────────────────────────────────────────────────

    [Fact]
    public async Task GetIssue_Returns200_WithIssueDetails()
    {
        _factory.JiraMock.Setup(c => c.GetIssueAsync("PROJ-1"))
            .ReturnsAsync(new JiraIssue { Key = "PROJ-1", Fields = new IssueFields { Summary = "Fix the bug" } });

        var response = await _http.GetAsync("/api/issues/PROJ-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var issue = await response.Content.ReadFromJsonAsync<JiraIssue>();
        Assert.Equal("PROJ-1", issue!.Key);
        Assert.Equal("Fix the bug", issue.Fields.Summary);
    }

    // ── POST /api/issues ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateIssue_Returns201_WithCreatedIssue()
    {
        _factory.JiraMock
            .Setup(c => c.CreateIssueAsync(It.Is<CreateIssueRequest>(r =>
                r.ProjectKey == "PROJ" && r.Summary == "New task")))
            .ReturnsAsync(new JiraIssue { Key = "PROJ-2" });

        var body = new CreateIssueRequest { ProjectKey = "PROJ", Summary = "New task" };
        var response = await _http.PostAsJsonAsync("/api/issues", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var issue = await response.Content.ReadFromJsonAsync<JiraIssue>();
        Assert.Equal("PROJ-2", issue!.Key);
    }

    // ── PUT /api/issues/{key} ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateIssue_Returns204()
    {
        _factory.JiraMock
            .Setup(c => c.UpdateIssueAsync("PROJ-1", It.IsAny<UpdateIssueRequest>()))
            .Returns(Task.CompletedTask);

        var body = new UpdateIssueRequest { Summary = "Updated summary" };
        var response = await _http.PutAsJsonAsync("/api/issues/PROJ-1", body);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ── POST /api/issues/{key}/transitions ────────────────────────────────────

    [Fact]
    public async Task TransitionIssue_Returns204()
    {
        _factory.JiraMock
            .Setup(c => c.TransitionIssueAsync("PROJ-1", "Done"))
            .Returns(Task.CompletedTask);

        var body = new TransitionRequest { TransitionName = "Done" };
        var response = await _http.PostAsJsonAsync("/api/issues/PROJ-1/transitions", body);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ── POST /api/issues/{key}/comments ───────────────────────────────────────

    [Fact]
    public async Task AddComment_Returns204()
    {
        _factory.JiraMock
            .Setup(c => c.AddCommentAsync("PROJ-1", "Great work!"))
            .Returns(Task.CompletedTask);

        var body = new AddCommentRequest { Text = "Great work!" };
        var response = await _http.PostAsJsonAsync("/api/issues/PROJ-1/comments", body);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
