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
}
