using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ProjectManagement.Discord.Services;

/// <summary>
/// Implements <see cref="ILlmChatService"/> using an OpenAI-compatible chat model.
/// Tool functions forward requests to the deployed Project Management REST API, so the LLM
/// can search issues, manage boards, and query repositories without any hard-coded commands.
/// </summary>
public sealed class LlmChatService : ILlmChatService
{
    private const string SystemPrompt =
        """
        You are a helpful project management assistant with access to Jira, GitHub, and Trello
        through tool functions. When a user asks you to perform an action or query data, use the
        appropriate tool. Always provide a clear, concise, human-readable response.
        Format lists using bullet points. Keep responses under 1800 characters.
        If a tool call fails, explain the error politely.
        """;

    private readonly IChatClient _chatClient;
    private readonly HttpClient  _httpClient;
    private readonly ILogger<LlmChatService> _logger;

    /// <summary>Initialises the service with the chat client and HTTP client for the deployed API.</summary>
    public LlmChatService(IChatClient chatClient, IHttpClientFactory httpClientFactory, ILogger<LlmChatService> logger)
    {
        _chatClient = chatClient;
        _httpClient = httpClientFactory.CreateClient(nameof(LlmChatService));
        _logger     = logger;
    }

    /// <inheritdoc />
    public async Task<string> AskAsync(string prompt, CancellationToken ct = default)
    {
        var tools = BuildTools();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User,   prompt),
        };

        var options = new ChatOptions
        {
            Tools    = tools,
            ToolMode = ChatToolMode.Auto,
        };

        _logger.LogDebug("Sending prompt to LLM: {Prompt}", prompt);
        var response = await _chatClient.GetResponseAsync(messages, options, ct);
        return response.Text ?? "Sorry, I couldn't generate a response.";
    }

    // ── Tool definitions ──────────────────────────────────────────────────────

    private List<AITool> BuildTools() =>
    [
        // Jira
        AIFunctionFactory.Create(SearchJiraIssuesAsync),
        AIFunctionFactory.Create(GetJiraIssueAsync),
        AIFunctionFactory.Create(CreateJiraIssueAsync),
        AIFunctionFactory.Create(AddJiraCommentAsync),
        AIFunctionFactory.Create(TransitionJiraIssueAsync),
        AIFunctionFactory.Create(GetJiraProjectsAsync),
        // GitHub
        AIFunctionFactory.Create(ListGitHubReposAsync),
        AIFunctionFactory.Create(GetGitHubRepoAsync),
        AIFunctionFactory.Create(ListGitHubIssuesAsync),
        AIFunctionFactory.Create(GetGitHubIssueAsync),
        // Trello
        AIFunctionFactory.Create(GetTrelloBoardsAsync),
        AIFunctionFactory.Create(GetTrelloBoardAsync),
        AIFunctionFactory.Create(GetTrelloCardsAsync),
        AIFunctionFactory.Create(GetTrelloCardAsync),
        AIFunctionFactory.Create(CreateTrelloCardAsync),
    ];

    // ── Jira tools ────────────────────────────────────────────────────────────

    [Description("Search for issues in a Jira project with optional filters")]
    private async Task<string> SearchJiraIssuesAsync(
        [Description("Jira project key (e.g. PROJ)")] string projectKey,
        [Description("Filter by status (e.g. 'In Progress', 'Done')")] string? status = null,
        [Description("Filter by issue type (e.g. Bug, Task, Story)")] string? issueType = null,
        [Description("Maximum number of results to return (default 25)")] int maxResults = 25)
    {
        var url = $"api/issues?projectKey={Uri.EscapeDataString(projectKey)}&maxResults={maxResults}";
        if (status    is not null) url += $"&status={Uri.EscapeDataString(status)}";
        if (issueType is not null) url += $"&issueType={Uri.EscapeDataString(issueType)}";
        return await GetApiResponseAsync(url);
    }

    [Description("Get full details of a specific Jira issue")]
    private async Task<string> GetJiraIssueAsync(
        [Description("Issue key (e.g. PROJ-123)")] string issueKey)
        => await GetApiResponseAsync($"api/issues/{Uri.EscapeDataString(issueKey)}");

    [Description("Create a new issue in a Jira project")]
    private async Task<string> CreateJiraIssueAsync(
        [Description("Target project key")] string projectKey,
        [Description("Issue summary / title")] string summary,
        [Description("Issue type (default: Task)")] string issueType = "Task",
        [Description("Optional description")] string? description = null,
        [Description("Optional priority (e.g. High, Medium, Low)")] string? priority = null)
    {
        var body = new
        {
            projectKey,
            summary,
            issueType,
            description,
            priority,
        };
        return await PostApiResponseAsync("api/issues", body);
    }

    [Description("Add a comment to a Jira issue")]
    private async Task<string> AddJiraCommentAsync(
        [Description("Issue key (e.g. PROJ-123)")] string issueKey,
        [Description("Comment text")] string comment)
        => await PostApiResponseAsync($"api/issues/{Uri.EscapeDataString(issueKey)}/comments",
            new { comment });

    [Description("Transition a Jira issue to a new workflow status (e.g. 'In Progress', 'Done')")]
    private async Task<string> TransitionJiraIssueAsync(
        [Description("Issue key (e.g. PROJ-123)")] string issueKey,
        [Description("Target status name (e.g. Done, In Progress, To Do)")] string transitionName)
        => await PostApiResponseAsync($"api/issues/{Uri.EscapeDataString(issueKey)}/transitions",
            new { transitionName });

    [Description("List all Jira projects accessible to the configured account")]
    private async Task<string> GetJiraProjectsAsync()
        => await GetApiResponseAsync("api/projects");

    // ── GitHub tools ──────────────────────────────────────────────────────────

    [Description("List GitHub repositories accessible to the configured token")]
    private async Task<string> ListGitHubReposAsync()
        => await GetApiResponseAsync("api/repositories");

    [Description("Get details for a specific GitHub repository")]
    private async Task<string> GetGitHubRepoAsync(
        [Description("Repository owner or organisation")] string owner,
        [Description("Repository name")] string repo)
        => await GetApiResponseAsync($"api/repositories/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}");

    [Description("List issues in a GitHub repository")]
    private async Task<string> ListGitHubIssuesAsync(
        [Description("Repository owner or organisation")] string owner,
        [Description("Repository name")] string repo,
        [Description("Issue state: open, closed, or all (default: open)")] string state = "open")
        => await GetApiResponseAsync($"api/repositories/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/issues?state={Uri.EscapeDataString(state)}");

    [Description("Get details for a specific GitHub issue")]
    private async Task<string> GetGitHubIssueAsync(
        [Description("Repository owner or organisation")] string owner,
        [Description("Repository name")] string repo,
        [Description("Issue number")] int issueNumber)
        => await GetApiResponseAsync($"api/repositories/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/issues/{issueNumber}");

    // ── Trello tools ──────────────────────────────────────────────────────────

    [Description("List all Trello boards accessible to the configured token")]
    private async Task<string> GetTrelloBoardsAsync()
        => await GetApiResponseAsync("api/boards");

    [Description("Get details for a specific Trello board")]
    private async Task<string> GetTrelloBoardAsync(
        [Description("Trello board ID")] string boardId)
        => await GetApiResponseAsync($"api/boards/{Uri.EscapeDataString(boardId)}");

    [Description("List all cards on a Trello board")]
    private async Task<string> GetTrelloCardsAsync(
        [Description("Trello board ID")] string boardId)
        => await GetApiResponseAsync($"api/boards/{Uri.EscapeDataString(boardId)}/cards");

    [Description("Get details for a specific Trello card")]
    private async Task<string> GetTrelloCardAsync(
        [Description("Trello card ID")] string cardId)
        => await GetApiResponseAsync($"api/cards/{Uri.EscapeDataString(cardId)}");

    [Description("Create a new card in a Trello list")]
    private async Task<string> CreateTrelloCardAsync(
        [Description("Trello list ID to add the card to")] string listId,
        [Description("Card title / name")] string name,
        [Description("Optional card description")] string? description = null)
        => await PostApiResponseAsync("api/cards", new { listId, name, description });

    // ── HTTP helpers ──────────────────────────────────────────────────────────

    private async Task<string> GetApiResponseAsync(string relativeUrl)
    {
        try
        {
            return await _httpClient.GetStringAsync(relativeUrl);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "API GET {Url} failed: {Status}", relativeUrl, ex.StatusCode);
            return ex.StatusCode == HttpStatusCode.NotFound
                ? "Not found."
                : $"API error: {ex.Message}";
        }
    }

    private async Task<string> PostApiResponseAsync(string relativeUrl, object body)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(relativeUrl, body);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "API POST {Url} failed: {Status}", relativeUrl, ex.StatusCode);
            return $"API error: {ex.Message}";
        }
    }
}
