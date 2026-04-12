using System.Net;
using System.Net.Http.Json;
using Moq;
using ProjectManagement.Core.Confluence.Models;

namespace ProjectManagement.Api.Tests.Confluence;

public class ConfluenceControllerTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _http;

    public ConfluenceControllerTests(ApiTestFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task CreatePage_Returns201()
    {
        _factory.ConfluenceMock
            .Setup(c => c.CreatePageAsync("ENG", "Doc", "<p>Body</p>", null))
            .ReturnsAsync(new ConfluencePage { Id = "100", Title = "Doc", Version = 1 });

        var response = await _http.PostAsJsonAsync("/api/confluence/pages", new
        {
            spaceKey = "ENG",
            title = "Doc",
            content = "<p>Body</p>",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetChildren_Returns200()
    {
        _factory.ConfluenceMock
            .Setup(c => c.GetChildrenAsync("123", 50))
            .ReturnsAsync([new ConfluencePage { Id = "124", Title = "Child", Version = 1 }]);

        var response = await _http.GetAsync("/api/confluence/pages/123/children");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pages = await response.Content.ReadFromJsonAsync<List<ConfluencePage>>();
        Assert.Single(pages!);
    }

    [Fact]
    public async Task DeletePage_Returns204()
    {
        _factory.ConfluenceMock.Setup(c => c.DeletePageAsync("123")).Returns(Task.CompletedTask);

        var response = await _http.DeleteAsync("/api/confluence/pages/123");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task CreateFolder_Returns201()
    {
        _factory.ConfluenceMock
            .Setup(c => c.CreateFolderAsync("ENG", "Folder", "100"))
            .ReturnsAsync(new ConfluencePage { Id = "101", Title = "Folder", Version = 1 });

        var response = await _http.PostAsJsonAsync("/api/confluence/pages/folders", new
        {
            spaceKey = "ENG",
            title = "Folder",
            parentPageId = "100",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task MovePage_Returns200()
    {
        _factory.ConfluenceMock
            .Setup(c => c.MovePageAsync("123", "456", false))
            .ReturnsAsync(new ConfluencePage { Id = "123", ParentId = "456", Version = 3 });

        var response = await _http.PutAsJsonAsync("/api/confluence/pages/123/move", new
        {
            newParentPageId = "456",
            minorEdit = false,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
