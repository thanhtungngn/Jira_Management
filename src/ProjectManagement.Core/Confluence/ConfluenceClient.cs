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

    public async Task<ConfluencePage> GetPageAsync(string pageId)
    {
        if (string.IsNullOrWhiteSpace(pageId))
            throw new ArgumentException("Confluence page ID is required.", nameof(pageId));

        _logger.LogDebug("Fetching Confluence page {PageId}", pageId);
        var response = await _httpClient.GetAsync($"content/{Uri.EscapeDataString(pageId)}?expand=version,space,ancestors,body.storage");
        await EnsureSuccessAsync(response);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var page = ParsePage(document.RootElement);
        _logger.LogInformation("Retrieved Confluence page {PageId} ({Title})", page.Id, page.Title);
        return page;
    }

    public async Task<List<ConfluencePage>> GetChildrenAsync(string pageId, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(pageId))
            throw new ArgumentException("Confluence page ID is required.", nameof(pageId));

        var safeLimit = Math.Clamp(limit, 1, 250);
        _logger.LogDebug("Fetching children for Confluence page {PageId}", pageId);
        var response = await _httpClient.GetAsync($"content/{Uri.EscapeDataString(pageId)}/child/page?limit={safeLimit}&expand=version,space,ancestors");
        await EnsureSuccessAsync(response);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var children = new List<ConfluencePage>();

        if (document.RootElement.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in results.EnumerateArray())
            {
                children.Add(ParsePage(item));
            }
        }

        _logger.LogInformation("Retrieved {Count} child pages for {PageId}", children.Count, pageId);
        return children;
    }

    public async Task<ConfluencePage> CreatePageAsync(
        string spaceKey,
        string title,
        string bodyStorageValue,
        string? parentPageId = null)
    {
        if (string.IsNullOrWhiteSpace(spaceKey))
            throw new ArgumentException("Confluence space key is required.", nameof(spaceKey));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Confluence page title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(bodyStorageValue))
            throw new ArgumentException("Confluence page body is required.", nameof(bodyStorageValue));

        var payload = new
        {
            type = "page",
            title,
            space = new { key = spaceKey },
            ancestors = string.IsNullOrWhiteSpace(parentPageId) ? null : new[] { new { id = parentPageId } },
            body = new
            {
                storage = new
                {
                    value = bodyStorageValue,
                    representation = "storage",
                },
            },
        };

        _logger.LogDebug("Creating Confluence page '{Title}' in space {SpaceKey}", title, spaceKey);
        var response = await _httpClient.PostAsJsonAsync("content", payload, JsonOptions);
        await EnsureSuccessAsync(response);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var page = ParsePage(document.RootElement);
        _logger.LogInformation("Created Confluence page {PageId} ({Title})", page.Id, page.Title);
        return page;
    }

    public async Task<ConfluencePage> CreateFolderAsync(string spaceKey, string title, string? parentPageId = null)
    {
        // Confluence Cloud has no native "folder" entity; use a container page for hierarchy.
        return await CreatePageAsync(spaceKey, title, "<p>Folder container page.</p>", parentPageId);
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

        var currentPage = await GetPageAsync(pageId);
        var currentTitle = currentPage.Title;
        var currentVersion = currentPage.Version;
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
        var updatedPage = ParsePage(updatedDocument.RootElement, nextVersion, title ?? currentTitle);

        _logger.LogInformation("Updated Confluence page {PageId} to version {Version}", updatedPage.Id, updatedPage.Version);
        return updatedPage;
    }

    public async Task<ConfluencePage> MovePageAsync(string pageId, string newParentPageId, bool minorEdit = false)
    {
        if (string.IsNullOrWhiteSpace(pageId))
            throw new ArgumentException("Confluence page ID is required.", nameof(pageId));
        if (string.IsNullOrWhiteSpace(newParentPageId))
            throw new ArgumentException("Confluence parent page ID is required.", nameof(newParentPageId));

        var currentPage = await GetPageAsync(pageId);
        var nextVersion = currentPage.Version + 1;

        var payload = new
        {
            id = pageId,
            type = "page",
            title = currentPage.Title,
            version = new
            {
                number = nextVersion,
                minorEdit,
            },
            ancestors = new[] { new { id = newParentPageId } },
        };

        _logger.LogDebug("Moving Confluence page {PageId} under parent {ParentPageId}", pageId, newParentPageId);
        var response = await _httpClient.PutAsJsonAsync($"content/{Uri.EscapeDataString(pageId)}", payload, JsonOptions);
        await EnsureSuccessAsync(response);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var movedPage = ParsePage(document.RootElement, nextVersion, currentPage.Title);
        _logger.LogInformation("Moved Confluence page {PageId} to parent {ParentPageId}", movedPage.Id, newParentPageId);
        return movedPage;
    }

    public async Task DeletePageAsync(string pageId)
    {
        if (string.IsNullOrWhiteSpace(pageId))
            throw new ArgumentException("Confluence page ID is required.", nameof(pageId));

        _logger.LogDebug("Deleting Confluence page {PageId}", pageId);
        var response = await _httpClient.DeleteAsync($"content/{Uri.EscapeDataString(pageId)}");
        await EnsureSuccessAsync(response);
        _logger.LogInformation("Deleted Confluence page {PageId}", pageId);
    }

    private static ConfluencePage ParsePage(JsonElement root, int? fallbackVersion = null, string? fallbackTitle = null)
    {
        var page = new ConfluencePage
        {
            Id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? string.Empty : string.Empty,
            Type = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() ?? "page" : "page",
            Status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() ?? string.Empty : string.Empty,
            Title = root.TryGetProperty("title", out var titleProp)
                ? titleProp.GetString() ?? (fallbackTitle ?? string.Empty)
                : (fallbackTitle ?? string.Empty),
            Version = root.TryGetProperty("version", out var versionProp)
                && versionProp.TryGetProperty("number", out var numberProp)
                    ? numberProp.GetInt32()
                    : (fallbackVersion ?? 0),
        };

        if (root.TryGetProperty("space", out var spaceProp)
            && spaceProp.TryGetProperty("key", out var spaceKeyProp))
        {
            page.SpaceKey = spaceKeyProp.GetString();
        }

        if (root.TryGetProperty("ancestors", out var ancestorsProp)
            && ancestorsProp.ValueKind == JsonValueKind.Array
            && ancestorsProp.GetArrayLength() > 0)
        {
            var last = ancestorsProp.EnumerateArray().Last();
            if (last.TryGetProperty("id", out var parentIdProp))
            {
                page.ParentId = parentIdProp.GetString();
            }
        }

        if (root.TryGetProperty("body", out var bodyProp)
            && bodyProp.TryGetProperty("storage", out var storageProp)
            && storageProp.TryGetProperty("value", out var valueProp))
        {
            page.BodyStorageValue = valueProp.GetString();
        }

        return page;
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
