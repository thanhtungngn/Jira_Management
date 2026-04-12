using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using ProjectManagement.Core.Confluence;

namespace ProjectManagement.Core.Tests.Confluence;

public class ConfluenceClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public async Task UpdatePageAsync_GetsCurrentVersion_ThenUpdatesPage()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        var getPayload = new
        {
            id = "12345",
            title = "Architecture",
            version = new { number = 7 },
        };

        var putPayload = new
        {
            id = "12345",
            type = "page",
            status = "current",
            title = "Architecture",
            version = new { number = 8 },
        };

        handlerMock
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(getPayload, JsonOptions), System.Text.Encoding.UTF8, "application/json"),
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(putPayload, JsonOptions), System.Text.Encoding.UTF8, "application/json"),
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://test.atlassian.net/wiki/rest/api/"),
        };

        var client = new ConfluenceClient(httpClient);

        var page = await client.UpdatePageAsync("12345", "<p>Updated content</p>");

        Assert.Equal("12345", page.Id);
        Assert.Equal("Architecture", page.Title);
        Assert.Equal(8, page.Version);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(2),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePageAsync_ThrowsHttpRequestException_OnApiError()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("{\"message\":\"Not found\"}"),
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://test.atlassian.net/wiki/rest/api/"),
        };

        var client = new ConfluenceClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.UpdatePageAsync("12345", "<p>Updated content</p>"));
    }

    [Fact]
    public async Task CreatePageAsync_ReturnsCreatedPage()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var payload = new
        {
            id = "20001",
            type = "page",
            status = "current",
            title = "New Doc",
            version = new { number = 1 },
            space = new { key = "ENG" },
        };

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), System.Text.Encoding.UTF8, "application/json"),
            });

        var client = new ConfluenceClient(new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://test.atlassian.net/wiki/rest/api/"),
        });

        var page = await client.CreatePageAsync("ENG", "New Doc", "<p>hello</p>");

        Assert.Equal("20001", page.Id);
        Assert.Equal("New Doc", page.Title);
        Assert.Equal("ENG", page.SpaceKey);
    }

    [Fact]
    public async Task GetChildrenAsync_ReturnsChildPages()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var payload = new
        {
            results = new object[]
            {
                new { id = "30001", type = "page", title = "Child A", version = new { number = 3 } },
                new { id = "30002", type = "page", title = "Child B", version = new { number = 1 } },
            },
        };

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get && r.RequestUri!.ToString().Contains("/child/page")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), System.Text.Encoding.UTF8, "application/json"),
            });

        var client = new ConfluenceClient(new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://test.atlassian.net/wiki/rest/api/"),
        });

        var children = await client.GetChildrenAsync("30000");

        Assert.Equal(2, children.Count);
        Assert.Equal("Child A", children[0].Title);
    }

    [Fact]
    public async Task MovePageAsync_ReturnsMovedPage()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        var getPayload = new
        {
            id = "12345",
            type = "page",
            title = "Architecture",
            version = new { number = 2 },
        };

        var putPayload = new
        {
            id = "12345",
            type = "page",
            status = "current",
            title = "Architecture",
            version = new { number = 3 },
            ancestors = new[] { new { id = "99999" } },
        };

        handlerMock
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(getPayload, JsonOptions), System.Text.Encoding.UTF8, "application/json"),
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(putPayload, JsonOptions), System.Text.Encoding.UTF8, "application/json"),
            });

        var client = new ConfluenceClient(new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://test.atlassian.net/wiki/rest/api/"),
        });

        var page = await client.MovePageAsync("12345", "99999");

        Assert.Equal("12345", page.Id);
        Assert.Equal(3, page.Version);
        Assert.Equal("99999", page.ParentId);
    }

    [Fact]
    public async Task DeletePageAsync_SendsDeleteRequest()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Delete && r.RequestUri!.ToString().Contains("content/12345")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent,
            });

        var client = new ConfluenceClient(new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://test.atlassian.net/wiki/rest/api/"),
        });

        await client.DeletePageAsync("12345");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetPageAsync_ReturnsPageWithBody()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var payload = new
        {
            id = "12345",
            type = "page",
            status = "current",
            title = "Architecture",
            version = new { number = 4 },
            body = new { storage = new { value = "<p>Body</p>" } },
        };

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), System.Text.Encoding.UTF8, "application/json"),
            });

        var client = new ConfluenceClient(new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://test.atlassian.net/wiki/rest/api/"),
        });

        var page = await client.GetPageAsync("12345");

        Assert.Equal("12345", page.Id);
        Assert.Equal("<p>Body</p>", page.BodyStorageValue);
    }

    [Fact]
    public async Task CreateFolderAsync_CreatesContainerPage()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var payload = new
        {
            id = "40001",
            type = "page",
            status = "current",
            title = "Folder",
            version = new { number = 1 },
        };

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), System.Text.Encoding.UTF8, "application/json"),
            });

        var client = new ConfluenceClient(new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://test.atlassian.net/wiki/rest/api/"),
        });

        var page = await client.CreateFolderAsync("ENG", "Folder", "100");

        Assert.Equal("40001", page.Id);
        Assert.Equal("Folder", page.Title);
    }
}
