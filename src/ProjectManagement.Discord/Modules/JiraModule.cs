using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using ProjectManagement.Discord.Services;

namespace ProjectManagement.Discord.Modules;

/// <summary>
/// Slash-command module that exposes Jira operations under the <c>/jira</c> group.
/// Each command delegates business logic to <see cref="IJiraService"/>.
/// </summary>
/// <remarks>
/// This class is a thin Discord.Net integration wrapper.
/// All testable business logic lives in <see cref="IJiraService"/> and its implementation.
/// </remarks>
[ExcludeFromCodeCoverage]
[Group("jira", "Jira project management commands")]
public sealed class JiraModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IJiraService _service;
    private readonly ILogger<JiraModule> _logger;

    /// <summary>Initialises the module with required services.</summary>
    public JiraModule(IJiraService service, ILogger<JiraModule> logger)
    {
        _service = service;
        _logger  = logger;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>Search issues in a Jira project with optional filters.</summary>
    [SlashCommand("search", "Search issues in a Jira project")]
    public async Task SearchAsync(
        [Summary("project_key", "Jira project key (e.g. PROJ)")] string projectKey,
        [Summary("status",      "Filter by status (e.g. 'In Progress')")] string? status    = null,
        [Summary("issue_type",  "Filter by issue type (e.g. Bug, Task)")] string? issueType = null)
    {
        // Defer the reply so we can take longer than 3 seconds to respond.
        await DeferAsync();
        _logger.LogDebug("Jira search: project={Project} status={Status} type={Type}", projectKey, status, issueType);
        var embed = await _service.SearchIssuesAsync(projectKey, status, issueType);
        await FollowupAsync(embed: embed);
    }

    /// <summary>Get the full details of a Jira issue.</summary>
    [SlashCommand("get", "Get details of a Jira issue")]
    public async Task GetAsync(
        [Summary("issue_key", "Issue key (e.g. PROJ-123)")] string issueKey)
    {
        await DeferAsync();
        _logger.LogDebug("Jira get: issue={IssueKey}", issueKey);
        var embed = await _service.GetIssueAsync(issueKey);
        await FollowupAsync(embed: embed);
    }

    /// <summary>Create a new issue in a Jira project.</summary>
    [SlashCommand("create", "Create a new Jira issue")]
    public async Task CreateAsync(
        [Summary("project_key",  "Target project key")] string projectKey,
        [Summary("summary",      "Issue summary / title")] string summary,
        [Summary("issue_type",   "Issue type (default: Task)")] string issueType    = "Task",
        [Summary("description",  "Optional description")]       string? description = null,
        [Summary("priority",     "Optional priority (e.g. High, Medium)")]  string? priority    = null)
    {
        await DeferAsync();
        _logger.LogDebug("Jira create: project={Project} type={Type} summary={Summary}", projectKey, issueType, summary);
        var embed = await _service.CreateIssueAsync(projectKey, summary, issueType, description, priority);
        await FollowupAsync(embed: embed);
    }

    /// <summary>Add a plain-text comment to a Jira issue.</summary>
    [SlashCommand("comment", "Add a comment to a Jira issue")]
    public async Task CommentAsync(
        [Summary("issue_key", "Issue key (e.g. PROJ-123)")] string issueKey,
        [Summary("text",      "Comment text")]               string text)
    {
        await DeferAsync();
        _logger.LogDebug("Jira comment: issue={IssueKey}", issueKey);
        var embed = await _service.AddCommentAsync(issueKey, text);
        await FollowupAsync(embed: embed);
    }

    /// <summary>Transition a Jira issue to a new workflow status.</summary>
    [SlashCommand("transition", "Move a Jira issue to a new status")]
    public async Task TransitionAsync(
        [Summary("issue_key",        "Issue key (e.g. PROJ-123)")] string issueKey,
        [Summary("transition_name",  "Target status (e.g. 'Done', 'In Progress')")] string transitionName)
    {
        await DeferAsync();
        _logger.LogDebug("Jira transition: issue={IssueKey} -> {Transition}", issueKey, transitionName);
        var embed = await _service.TransitionIssueAsync(issueKey, transitionName);
        await FollowupAsync(embed: embed);
    }

    /// <summary>List all accessible Jira projects.</summary>
    [SlashCommand("projects", "List all accessible Jira projects")]
    public async Task ProjectsAsync()
    {
        await DeferAsync();
        _logger.LogDebug("Jira projects");
        var embed = await _service.GetProjectsAsync();
        await FollowupAsync(embed: embed);
    }
}
