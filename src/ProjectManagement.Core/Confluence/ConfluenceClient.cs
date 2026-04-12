using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectManagement.Core.Confluence.Models;

namespace ProjectManagement.Core.Confluence;

public class ConfluenceClient : IConfluenceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ConfluenceClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public ConfluenceClient(HttpClient httpClient, ILogger<ConfluenceClient>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger ?? NullLogger<ConfluenceClient>.Instance;
    }

    public static ConfluenceClient Create(string baseUrl, string email, string apiToken)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Confluence base URL is required.", nameof(baseUrl));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Confluence email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(apiToken))
            throw new ArgumentException("Confluence API token is required.", nameof(apiToken));

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{apiToken}"));
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/wiki/rest/api/"),
        };
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        return new ConfluenceClient(httpClient);
    }

    public async Task<ConfluencePage> UpdatePageAsync(
        string pageId,
        string bodyStorageValue,
        string? title = null,
        bool minorEdit = false)
    {
        if (string.IsNullOrWhiteSpace(pageId))
            throw new ArgumentException("Confluence page ID is required.", nameof(pageId));
        if (string.IsNullOrWhiteSpace(bodyStorageValue))
            throw new ArgumentException("Confluence page body is required.", nameof(bodyStorageValue));

        _logger.LogDebug("Fetching Confluence page {PageId} before update", pageId);
        var pageResponse = await _httpClient.GetAsync($"content/{Uri.EscapeDataString(pageId)}?expand=version");
        await EnsureSuccessAsync(pageResponse);

        using var pageDocument = await JsonDocument.ParseAsync(await pageResponse.Content.ReadAsStreamAsync());
        var root = pageDocument.RootElement;

        var currentTitle = root.TryGetProperty("title", out var titleProp)
            ? titleProp.GetString() ?? string.Empty
            : string.Empty;
        var currentVersion = root.GetProperty("version").GetProperty("number").GetInt32();
        var nextVersion = currentVersion + 1;

        var updatePayload = new
        {
            id = pageId,
            type = "page",
            title = string.IsNullOrWhiteSpace(title) ? currentTitle : title,
            version = new
            {
                number = nextVersion,
                minorEdit,
            },
            body = new
            {
                storage = new
                {
                    value = bodyStorageValue,
                    representation = "storage",
                },
            },
        };

        _logger.LogDebug("Updating Confluence page {PageId} to version {Version}", pageId, nextVersion);
        var updateResponse = await _httpClient.PutAsJsonAsync($"content/{Uri.EscapeDataString(pageId)}", updatePayload, JsonOptions);
        await EnsureSuccessAsync(updateResponse);

        using var updatedDocument = await JsonDocument.ParseAsync(await updateResponse.Content.ReadAsStreamAsync());
        var updatedRoot = updatedDocument.RootElement;

        var updatedPage = new ConfluencePage
        {
            Id = updatedRoot.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? pageId : pageId,
            Type = updatedRoot.TryGetProperty("type", out var typeProp) ? typeProp.GetString() ?? "page" : "page",
            Status = updatedRoot.TryGetProperty("status", out var statusProp) ? statusProp.GetString() ?? string.Empty : string.Empty,
            Title = updatedRoot.TryGetProperty("title", out var updatedTitleProp)
                ? updatedTitleProp.GetString() ?? (title ?? currentTitle)
                : (title ?? currentTitle),
            Version = updatedRoot.TryGetProperty("version", out var versionProp)
                && versionProp.TryGetProperty("number", out var numberProp)
                    ? numberProp.GetInt32()
                    : nextVersion,
        };

        _logger.LogInformation("Updated Confluence page {PageId} to version {Version}", updatedPage.Id, updatedPage.Version);
        return updatedPage;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Confluence API error {(int)response.StatusCode} ({response.ReasonPhrase}): {body}");
        }
    }
}
