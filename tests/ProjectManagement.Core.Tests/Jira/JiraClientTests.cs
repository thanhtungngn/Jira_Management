using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Moq;
using Moq.Protected;
using ProjectManagement.Core.Jira;
using ProjectManagement.Core.Jira.Models;

namespace ProjectManagement.Core.Tests.Jira;

/// <summary>
/// Unit tests for <see cref="JiraClient"/> that use a mocked <see cref="HttpMessageHandler"/>
/// so no real network calls are made.
/// </summary>
public class JiraClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (JiraClient client, Mock<HttpMessageHandler> handlerMock) CreateClient(
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
            BaseAddress = new Uri("https://test.atlassian.net/rest/api/3/"),
        };

        return (new JiraClient(httpClient), handlerMock);
    }

    private static (JiraClient client, Mock<HttpMessageHandler> handlerMock) CreateClientForSequence(
        params (HttpStatusCode statusCode, object body)[] responses)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var queue = new Queue<(HttpStatusCode, object)>(responses);

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var (code, body) = queue.Dequeue();
                var json = JsonSerializer.Serialize(body, JsonOptions);
                return new HttpResponseMessage
                {
                    StatusCode = code,
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
                };
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://test.atlassian.net/rest/api/3/"),
        };

        return (new JiraClient(httpClient), handlerMock);
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Create_ThrowsArgumentException_WhenBaseUrlIsEmpty()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            JiraClient.Create("", "email@example.com", "token"));
        Assert.Contains("base URL", ex.Message);
    }

    [Fact]
    public void Create_ThrowsArgumentException_WhenEmailIsEmpty()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            JiraClient.Create("https://test.atlassian.net", "", "token"));
        Assert.Contains("email", ex.Message);
    }

    [Fact]
    public void Create_ThrowsArgumentException_WhenApiTokenIsEmpty()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            JiraClient.Create("https://test.atlassian.net", "email@example.com", ""));
        Assert.Contains("API token", ex.Message);
    }

    // ── GetProjectsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetProjectsAsync_ReturnsProjects()
    {
        var payload = new
        {
            values = new[]
            {
                new { id = "10000", key = "PROJ", name = "My Project", projectTypeKey = "software" },
            },
            total = 1,
        };
        var (client, _) = CreateClient(HttpStatusCode.OK, payload);

        var projects = await client.GetProjectsAsync();

        Assert.Single(projects);
        Assert.Equal("PROJ", projects[0].Key);
        Assert.Equal("My Project", projects[0].Name);
    }

    [Fact]
    public async Task GetProjectsAsync_ReturnsEmpty_WhenNoProjects()
    {
        var payload = new { values = Array.Empty<object>(), total = 0 };
        var (client, _) = CreateClient(HttpStatusCode.OK, payload);

        var projects = await client.GetProjectsAsync();

        Assert.Empty(projects);
    }

    [Fact]
    public async Task GetProjectsAsync_ThrowsHttpRequestException_OnApiError()
    {
        var payload = new { errorMessages = new[] { "Not found" } };
        var (client, _) = CreateClient(HttpStatusCode.Unauthorized, payload);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetProjectsAsync());
    }

    // ── GetProjectAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetProjectAsync_ReturnsProject()
    {
        var payload = new { id = "10000", key = "PROJ", name = "My Project", projectTypeKey = "software" };
        var (client, _) = CreateClient(HttpStatusCode.OK, payload);

        var project = await client.GetProjectAsync("PROJ");

        Assert.Equal("PROJ", project.Key);
        Assert.Equal("My Project", project.Name);
    }

    // ── SearchIssuesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task SearchIssuesAsync_ReturnsIssues()
    {
        var payload = new
        {
            issues = new[]
            {
                new
                {
                    id = "10001",
                    key = "PROJ-1",
                    self = "https://test.atlassian.net/rest/api/3/issue/10001",
                    fields = new
                    {
                        summary = "Fix the bug",
                        status = new { name = "To Do" },
                        issuetype = new { name = "Bug" },
                        priority = new { name = "High" },
                        assignee = (object?)null,
                        reporter = (object?)null,
                    },
                },
            },
            total = 1,
            startAt = 0,
            maxResults = 50,
        };
        var (client, _) = CreateClient(HttpStatusCode.OK, payload);

        var result = await client.SearchIssuesAsync(new SearchIssuesRequest { ProjectKey = "PROJ" });

        Assert.Single(result.Issues);
        Assert.Equal("PROJ-1", result.Issues[0].Key);
        Assert.Equal("Fix the bug", result.Issues[0].Fields.Summary);
        Assert.Equal(1, result.Total);
    }

    [Fact]
    public async Task SearchIssuesAsync_AppliesFilters()
    {
        var payload = new { issues = Array.Empty<object>(), total = 0, startAt = 0, maxResults = 50 };
        var (client, handlerMock) = CreateClient(HttpStatusCode.OK, payload);

        await client.SearchIssuesAsync(new SearchIssuesRequest
        {
            ProjectKey    = "PROJ",
            Status        = "In Progress",
            IssueType     = "Bug",
            AssigneeEmail = "dev@example.com",
        });

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.Query.Contains("In%20Progress") &&
                req.RequestUri.Query.Contains("Bug") &&
                req.RequestUri.Query.Contains("dev%40example.com")),
            ItExpr.IsAny<CancellationToken>());
    }

    // ── GetIssueAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetIssueAsync_ReturnsIssueDetails()
    {
        var payload = new
        {
            id = "10001",
            key = "PROJ-1",
            self = "https://test.atlassian.net/rest/api/3/issue/10001",
            fields = new
            {
                summary = "Fix the bug",
                status = new { name = "In Progress" },
                issuetype = new { name = "Bug" },
                priority = new { name = "High" },
                assignee = new { accountId = "abc", displayName = "Alice", emailAddress = "alice@example.com" },
                reporter = (object?)null,
                comment = new { comments = Array.Empty<object>(), total = 0 },
            },
        };
        var (client, _) = CreateClient(HttpStatusCode.OK, payload);

        var issue = await client.GetIssueAsync("PROJ-1");

        Assert.Equal("PROJ-1", issue.Key);
        Assert.Equal("Fix the bug", issue.Fields.Summary);
        Assert.Equal("In Progress", issue.Fields.Status.Name);
        Assert.Equal("Alice", issue.Fields.Assignee?.DisplayName);
    }

    // ── CreateIssueAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateIssueAsync_ReturnsCreatedIssue()
    {
        var payload = new
        {
            id = "10002",
            key = "PROJ-2",
            self = "https://test.atlassian.net/rest/api/3/issue/10002",
            fields = new
            {
                summary = "New feature",
                status = new { name = "To Do" },
                issuetype = new { name = "Story" },
            },
        };
        var (client, _) = CreateClient(HttpStatusCode.Created, payload);

        var issue = await client.CreateIssueAsync(new CreateIssueRequest
        {
            ProjectKey = "PROJ",
            Summary    = "New feature",
            IssueType  = "Story",
        });

        Assert.Equal("PROJ-2", issue.Key);
    }

    // ── UpdateIssueAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateIssueAsync_SendsPutRequest()
    {
        var (client, handlerMock) = CreateClient(HttpStatusCode.NoContent, new { });

        await client.UpdateIssueAsync("PROJ-1", new UpdateIssueRequest { Summary = "Updated summary" });

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Put &&
                req.RequestUri!.ToString().Contains("PROJ-1")),
            ItExpr.IsAny<CancellationToken>());
    }

    // ── AddCommentAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task AddCommentAsync_SendsPostRequest()
    {
        var payload = new
        {
            id = "1",
            author = new { accountId = "abc", displayName = "Alice", emailAddress = "alice@example.com" },
            body = new { version = 1, type = "doc", content = Array.Empty<object>() },
            created = DateTime.UtcNow,
            updated = DateTime.UtcNow,
        };
        var (client, handlerMock) = CreateClient(HttpStatusCode.Created, payload);

        await client.AddCommentAsync("PROJ-1", "This is a comment.");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri!.ToString().Contains("PROJ-1/comment")),
            ItExpr.IsAny<CancellationToken>());
    }

    // ── TransitionIssueAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task TransitionIssueAsync_TransitionsToMatchingName()
    {
        var transitionsPayload = new
        {
            transitions = new[]
            {
                new { id = "11", name = "To Do" },
                new { id = "21", name = "In Progress" },
                new { id = "31", name = "Done" },
            },
        };
        var (client, handlerMock) = CreateClientForSequence(
            (HttpStatusCode.OK, transitionsPayload),
            (HttpStatusCode.NoContent, new { }));

        await client.TransitionIssueAsync("PROJ-1", "In Progress");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(2),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task TransitionIssueAsync_ThrowsInvalidOperation_WhenTransitionNotFound()
    {
        var transitionsPayload = new
        {
            transitions = new[] { new { id = "11", name = "To Do" } },
        };
        var (client, _) = CreateClient(HttpStatusCode.OK, transitionsPayload);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.TransitionIssueAsync("PROJ-1", "NonExistentTransition"));

        Assert.Contains("NonExistentTransition", ex.Message);
        Assert.Contains("To Do", ex.Message);
    }
}
