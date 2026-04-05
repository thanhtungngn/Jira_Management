using System.ComponentModel;
using ModelContextProtocol.Server;
using ProjectManagement.Core.Jira;
using ProjectManagement.Core.Jira.Models;

namespace ProjectManagement.Mcp.Jira;

[McpServerToolType]
public sealed class JiraTools
{
    private readonly IJiraClient _client;

    public JiraTools(IJiraClient client)
    {
        _client = client;
    }

    [McpServerTool(Name = "search_issues"), Description("Search Jira issues in a project using optional filters (status, issue type, assignee).")]
    public async Task<SearchResult> SearchIssuesAsync(
        string projectKey,
        string? status = null,
        string? issueType = null,
        string? assigneeEmail = null,
        int maxResults = 50,
        int startAt = 0)
    {
        return await _client.SearchIssuesAsync(new SearchIssuesRequest
        {
            ProjectKey    = projectKey,
            Status        = status,
            IssueType     = issueType,
            AssigneeEmail = assigneeEmail,
            MaxResults    = maxResults,
            StartAt       = startAt,
        });
    }

    [McpServerTool(Name = "get_issue"), Description("Get full details of a Jira issue including comments.")]
    public async Task<JiraIssue> GetIssueAsync(string issueKey)
    {
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
        await _client.TransitionIssueAsync(issueKey, transitionName);
    }

    [McpServerTool(Name = "add_comment"), Description("Add a plain-text comment to a Jira issue.")]
    public async Task AddCommentAsync(string issueKey, string comment)
    {
        await _client.AddCommentAsync(issueKey, comment);
    }

    [McpServerTool(Name = "get_projects"), Description("List all accessible Jira projects.")]
    public async Task<List<JiraProject>> GetProjectsAsync()
    {
        return await _client.GetProjectsAsync();
    }
}
