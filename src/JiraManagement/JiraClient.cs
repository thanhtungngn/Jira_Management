using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using JiraManagement.Models;

namespace JiraManagement;

public class JiraClient : IJiraClient
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public JiraClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
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

    public async Task<List<JiraProject>> GetProjectsAsync()
    {
        var response = await _httpClient.GetAsync("project/search?maxResults=50");
        await EnsureSuccessAsync(response);
        var result = await response.Content.ReadFromJsonAsync<PagedProjects>(JsonOptions);
        return result?.Values ?? [];
    }

    public async Task<JiraProject> GetProjectAsync(string projectKey)
    {
        var response = await _httpClient.GetAsync($"project/{Uri.EscapeDataString(projectKey)}");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<JiraProject>(JsonOptions)
               ?? throw new InvalidOperationException("Empty response from Jira.");
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
        var fields = "summary,status,issuetype,priority,assignee,reporter,created,updated";
        var url = $"search?jql={Uri.EscapeDataString(jql)}&maxResults={request.MaxResults}&startAt={request.StartAt}&fields={fields}";

        var response = await _httpClient.GetAsync(url);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<SearchResult>(JsonOptions)
               ?? new SearchResult();
    }

    public async Task<JiraIssue> GetIssueAsync(string issueKey)
    {
        var fields = "summary,description,status,issuetype,priority,assignee,reporter,created,updated,comment";
        var response = await _httpClient.GetAsync(
            $"issue/{Uri.EscapeDataString(issueKey)}?fields={fields}");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<JiraIssue>(JsonOptions)
               ?? throw new InvalidOperationException("Empty response from Jira.");
    }

    public async Task<JiraIssue> CreateIssueAsync(CreateIssueRequest request)
    {
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
        return await response.Content.ReadFromJsonAsync<JiraIssue>(JsonOptions)
               ?? throw new InvalidOperationException("Empty response from Jira.");
    }

    public async Task UpdateIssueAsync(string issueKey, UpdateIssueRequest request)
    {
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
    }

    public async Task AddCommentAsync(string issueKey, string comment)
    {
        var body = new { body = BuildAdf(comment) };
        var response = await _httpClient.PostAsJsonAsync(
            $"issue/{Uri.EscapeDataString(issueKey)}/comment", body, JsonOptions);
        await EnsureSuccessAsync(response);
    }

    public async Task TransitionIssueAsync(string issueKey, string transitionName)
    {
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
