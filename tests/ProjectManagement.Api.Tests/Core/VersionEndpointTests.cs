using System.Net;
using System.Text.Json;

namespace ProjectManagement.Api.Tests.Core;

public class VersionEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _http;

    public VersionEndpointTests(ApiTestFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Theory]
    [InlineData("/version")]
    [InlineData("/api/version")]
    public async Task VersionEndpoints_Return200_WithVersionPayload(string route)
    {
        var response = await _http.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("service", out var serviceProp));
        Assert.Equal("ProjectManagement.Api", serviceProp.GetString());

        Assert.True(root.TryGetProperty("version", out var versionProp));
        Assert.False(string.IsNullOrWhiteSpace(versionProp.GetString()));
    }
}
