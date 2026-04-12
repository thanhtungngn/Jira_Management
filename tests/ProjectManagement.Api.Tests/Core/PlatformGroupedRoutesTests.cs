using System.Net;
using System.Net.Http.Json;
using Moq;
using ProjectManagement.Core.Confluence.Models;
using ProjectManagement.Core.GitHub.Models;
using ProjectManagement.Core.Jira.Models;
using ProjectManagement.Core.Trello.Models;

namespace ProjectManagement.Api.Tests.Core;

public class PlatformGroupedRoutesTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _http;

    public PlatformGroupedRoutesTests(ApiTestFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task JiraGroupedRoute_Works()
    {
        _factory.JiraMock.Setup(c => c.GetProjectsAsync())
            .ReturnsAsync([new JiraProject { Key = "PROJ", Name = "My Project" }]);

        var response = await _http.GetAsync("/api/jira/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<JiraProject>>();
        Assert.Single(body!);
    }

    [Fact]
    public async Task TrelloGroupedRoute_Works()
    {
        _factory.TrelloMock.Setup(c => c.GetBoardsAsync())
            .ReturnsAsync([new TrelloBoard { Id = "b1", Name = "Board" }]);

        var response = await _http.GetAsync("/api/trello/boards");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<TrelloBoard>>();
        Assert.Single(body!);
    }

    [Fact]
    public async Task GitHubGroupedRoute_Works()
    {
        _factory.GitHubMock.Setup(c => c.ListRepositoriesAsync())
            .ReturnsAsync([new GitHubRepository { FullName = "owner/repo", Name = "repo" }]);

        var response = await _http.GetAsync("/api/github/repositories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<GitHubRepository>>();
        Assert.Single(body!);
    }

    [Fact]
    public async Task ConfluenceGroupedRoute_Works()
    {
        _factory.ConfluenceMock
            .Setup(c => c.UpdatePageAsync("123", "<p>updated</p>", "My Page", false))
            .ReturnsAsync(new ConfluencePage { Id = "123", Title = "My Page", Version = 2, Type = "page", Status = "current" });

        var payload = new
        {
            content = "<p>updated</p>",
            title = "My Page",
            minorEdit = false,
        };

        var response = await _http.PutAsJsonAsync("/api/confluence/pages/123", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ConfluencePage>();
        Assert.Equal("123", body!.Id);
        Assert.Equal(2, body.Version);
    }
}
