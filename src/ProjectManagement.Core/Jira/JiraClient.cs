using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectManagement.Core.Jira.Models;

namespace ProjectManagement.Core.Jira;

public class JiraClient : IJiraClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JiraClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters             = { new JiraDateTimeConverter() },
    };

    public JiraClient(HttpClient httpClient, ILogger<JiraClient>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger ?? NullLogger<JiraClient>.Instance;
    }

    public static JiraClient Create(string baseUrl, string email, string apiToken)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Jira base URL is required.", nameof(baseUrl));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Jira email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(apiToken))
            throw new ArgumentException("Jira API token is required.", nameof(apiToken));

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{apiToken}"));
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/rest/api/3/"),
        };
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        return new JiraClient(httpClient);
    }

    public async Task<JiraUser> GetCurrentUserAsync()
    {
        _logger.LogDebug("Fetching current Jira user");
        var response = await _httpClient.GetAsync("myself");
        await EnsureSuccessAsync(response);
        var user = await response.Content.ReadFromJsonAsync<JiraUser>(JsonOptions)
               ?? throw new InvalidOperationException("Empty response from Jira.");
        _logger.LogInformation("Authenticated as {DisplayName} ({Email})", user.DisplayName, user.EmailAddress);
        return user;
    }

    public async Task<List<JiraProject>> GetProjectsAsync()
    {
        _logger.LogDebug("Fetching Jira projects");
        var response = await _httpClient.GetAsync("project/search?maxResults=50");
        await EnsureSuccessAsync(response);
        var result = await response.Content.ReadFromJsonAsync<PagedProjects>(JsonOptions);
        var projects = result?.Values ?? [];
        _logger.LogInformation("Retrieved {Count} projects", projects.Count);
        return projects;
    }

    public async Task<JiraProject> GetProjectAsync(string projectKey)
    {
        _logger.LogDebug("Fetching project {ProjectKey}", projectKey);
        var response = await _httpClient.GetAsync($"project/{Uri.EscapeDataString(projectKey)}");
        await EnsureSuccessAsync(response);
        var project = await response.Content.ReadFromJsonAsync<JiraProject>(JsonOptions)
               ?? throw new InvalidOperationException("Empty response from Jira.");
        _logger.LogInformation("Retrieved project {ProjectKey} ({ProjectName})", project.Key, project.Name);
        return project;
    }

    public async Task<SearchResult> SearchIssuesAsync(SearchIssuesRequest request)
    {
        var jqlParts = new List<string> { $"project = {request.ProjectKey}" };

        if (!string.IsNullOrWhiteSpace(request.Status))
            jqlParts.Add($"status = \"{request.Status}\"");
        if (!string.IsNullOrWhiteSpace(request.IssueType))
            jqlParts.Add($"issuetype = \"{request.IssueType}\"");
        if (!string.IsNullOrWhiteSpace(request.AssigneeEmail))
            jqlParts.Add($"assignee = \"{request.AssigneeEmail}\"");

        var jql = string.Join(" AND ", jqlParts);
        _logger.LogDebug("Searching issues with JQL: {Jql}", jql);

        // GET /rest/api/3/search was removed (410 Gone).
        // Use POST /rest/api/3/search/jql per Atlassian changelog CHANGE-2046.
        // The new endpoint uses nextPageToken (string) for pagination, not startAt (int).
        var body = new
        {
            jql,
            maxResults     = request.MaxResults,
            nextPageToken  = request.NextPageToken,   // omitted when null (WhenWritingNull)
            fields         = new[] { "summary", "status", "issuetype", "priority",
                                     "assignee", "reporter", "created", "updated" },
        };

        var response = await _httpClient.PostAsJsonAsync("search/jql", body, JsonOptions);
        await EnsureSuccessAsync(response);
        var result = await response.Content.ReadFromJsonAsync<SearchResult>(JsonOptions)
               ?? new SearchResult();
        _logger.LogInformation("Found {Total} issues for project {ProjectKey} (returned {Count})",
            result.Total, request.ProjectKey, result.Issues?.Count ?? 0);
        return result;
    }

    public async Task<JiraIssue> GetIssueAsync(string issueKey)
    {
        _logger.LogDebug("Fetching issue {IssueKey}", issueKey);
        var fields = "summary,description,status,issuetype,priority,assignee,reporter,created,updated,comment";
        var response = await _httpClient.GetAsync(
            $"issue/{Uri.EscapeDataString(issueKey)}?fields={fields}");
        await EnsureSuccessAsync(response);
        var issue = await response.Content.ReadFromJsonAsync<JiraIssue>(JsonOptions)
               ?? throw new InvalidOperationException("Empty response from Jira.");
        _logger.LogInformation("Retrieved issue {IssueKey}", issue.Key);
        return issue;
    }

    public async Task<JiraIssue> CreateIssueAsync(CreateIssueRequest request)
    {
        _logger.LogDebug("Creating issue in project {ProjectKey}: {Summary}", request.ProjectKey, request.Summary);
        var fields = new Dictionary<string, object>
        {
            ["project"] = new { key = request.ProjectKey },
            ["summary"] = request.Summary,
            ["issuetype"] = new { name = request.IssueType },
        };

        if (!string.IsNullOrWhiteSpace(request.Description))
            fields["description"] = BuildAdf(request.Description);

        if (!string.IsNullOrWhiteSpace(request.Priority))
            fields["priority"] = new { name = request.Priority };

        if (!string.IsNullOrWhiteSpace(request.AssigneeAccountId))
            fields["assignee"] = new { accountId = request.AssigneeAccountId };

        var body = new { fields };
        var response = await _httpClient.PostAsJsonAsync("issue", body, JsonOptions);
        await EnsureSuccessAsync(response);
        var issue = await response.Content.ReadFromJsonAsync<JiraIssue>(JsonOptions)
               ?? throw new InvalidOperationException("Empty response from Jira.");
        _logger.LogInformation("Created issue {IssueKey} in project {ProjectKey}", issue.Key, request.ProjectKey);
        return issue;
    }

    public async Task UpdateIssueAsync(string issueKey, UpdateIssueRequest request)
    {
        _logger.LogDebug("Updating issue {IssueKey}", issueKey);
        var fields = new Dictionary<string, object>();

        if (request.Summary is not null)
            fields["summary"] = request.Summary;
        if (request.Description is not null)
            fields["description"] = BuildAdf(request.Description);
        if (request.Priority is not null)
            fields["priority"] = new { name = request.Priority };
        if (request.AssigneeAccountId is not null)
            fields["assignee"] = new { accountId = request.AssigneeAccountId };

        var body = new { fields };
        var response = await _httpClient.PutAsJsonAsync(
            $"issue/{Uri.EscapeDataString(issueKey)}", body, JsonOptions);
        await EnsureSuccessAsync(response);
        _logger.LogInformation("Updated issue {IssueKey}", issueKey);
    }

    public async Task AddCommentAsync(string issueKey, string comment)
    {
        _logger.LogDebug("Adding comment to issue {IssueKey}", issueKey);
        var body = new { body = BuildAdf(comment) };
        var response = await _httpClient.PostAsJsonAsync(
            $"issue/{Uri.EscapeDataString(issueKey)}/comment", body, JsonOptions);
        await EnsureSuccessAsync(response);
        _logger.LogInformation("Added comment to issue {IssueKey}", issueKey);
    }

    public async Task TransitionIssueAsync(string issueKey, string transitionName)
    {
        _logger.LogDebug("Transitioning issue {IssueKey} to '{TransitionName}'", issueKey, transitionName);
        var transitionsResponse = await _httpClient.GetAsync(
            $"issue/{Uri.EscapeDataString(issueKey)}/transitions");
        await EnsureSuccessAsync(transitionsResponse);

        var result = await transitionsResponse.Content.ReadFromJsonAsync<TransitionsResult>(JsonOptions)
                     ?? new TransitionsResult();

        var transition = result.Transitions.FirstOrDefault(
            t => string.Equals(t.Name, transitionName, StringComparison.OrdinalIgnoreCase));

        if (transition is null)
        {
            var available = string.Join(", ", result.Transitions.Select(t => t.Name));
            throw new InvalidOperationException(
                $"Transition \"{transitionName}\" not found. Available: {available}");
        }

        var body = new { transition = new { id = transition.Id } };
        var response = await _httpClient.PostAsJsonAsync(
            $"issue/{Uri.EscapeDataString(issueKey)}/transitions", body, JsonOptions);
        await EnsureSuccessAsync(response);
        _logger.LogInformation("Transitioned issue {IssueKey} to '{TransitionName}'", issueKey, transitionName);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static object BuildAdf(string text) => new
    {
        version = 1,
        type = "doc",
        content = new[]
        {
            new
            {
                type = "paragraph",
                content = new[] { new { type = "text", text } },
            },
        },
    };

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Jira API error {(int)response.StatusCode} ({response.ReasonPhrase}): {body}");
        }
    }
}
