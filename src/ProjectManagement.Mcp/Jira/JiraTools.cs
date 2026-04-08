using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using ProjectManagement.Core.Jira;
using ProjectManagement.Core.Jira.Models;

namespace ProjectManagement.Mcp.Jira;

[McpServerToolType]
public sealed class JiraTools
{
    private readonly IJiraClient _client;
    private readonly ILogger<JiraTools> _logger;

    public JiraTools(IJiraClient client, ILogger<JiraTools> logger)
    {
        _client = client;
        _logger = logger;
    }

    [McpServerTool(Name = "search_issues"), Description("Search Jira issues in a project using optional filters (status, issue type, assignee).")]
    public async Task<SearchResult> SearchIssuesAsync(
        string projectKey,
        string? status = null,
        string? issueType = null,
        string? assigneeEmail = null,
        int maxResults = 50,
        string? nextPageToken = null)
    {
        _logger.LogInformation("[MCP] search_issues: project={ProjectKey} status={Status} type={IssueType}", projectKey, status, issueType);
        return await _client.SearchIssuesAsync(new SearchIssuesRequest
        {
            ProjectKey    = projectKey,
            Status        = status,
            IssueType     = issueType,
            AssigneeEmail = assigneeEmail,
            MaxResults    = maxResults,
            NextPageToken = nextPageToken,
        });
    }

    [McpServerTool(Name = "get_issue"), Description("Get full details of a Jira issue including comments.")]
    public async Task<JiraIssue> GetIssueAsync(string issueKey)
    {
        _logger.LogInformation("[MCP] get_issue: {IssueKey}", issueKey);
        return await _client.GetIssueAsync(issueKey);
    }

    [McpServerTool(Name = "create_issue"), Description("Create a new Jira issue in a project.")]
    public async Task<JiraIssue> CreateIssueAsync(
        string projectKey,
        string summary,
        string issueType = "Task",
        string? description = null,
        string? priority = null,
        string? assigneeAccountId = null)
    {
        _logger.LogInformation("[MCP] create_issue: project={ProjectKey} type={IssueType} summary={Summary}", projectKey, issueType, summary);
        return await _client.CreateIssueAsync(new CreateIssueRequest
        {
            ProjectKey        = projectKey,
            Summary           = summary,
            IssueType         = issueType,
            Description       = description,
            Priority          = priority,
            AssigneeAccountId = assigneeAccountId,
        });
    }

    [McpServerTool(Name = "transition_issue"), Description("Transition a Jira issue to a new workflow status (e.g. 'In Progress', 'Done').")]
    public async Task TransitionIssueAsync(string issueKey, string transitionName)
    {
        _logger.LogInformation("[MCP] transition_issue: {IssueKey} -> {Transition}", issueKey, transitionName);
        await _client.TransitionIssueAsync(issueKey, transitionName);
    }

    [McpServerTool(Name = "add_comment"), Description("Add a plain-text comment to a Jira issue.")]
    public async Task AddCommentAsync(string issueKey, string comment)
    {
        _logger.LogInformation("[MCP] add_comment: {IssueKey}", issueKey);
        await _client.AddCommentAsync(issueKey, comment);
    }

    [McpServerTool(Name = "get_projects"), Description("List all accessible Jira projects.")]
    public async Task<List<JiraProject>> GetProjectsAsync()
    {
        _logger.LogInformation("[MCP] get_projects");
        return await _client.GetProjectsAsync();
    }
}
