using Discord;
using Microsoft.Extensions.Logging;
using ProjectManagement.Core.Jira;
using ProjectManagement.Core.Jira.Models;
using ProjectManagement.Discord.Formatting;

namespace ProjectManagement.Discord.Services;

/// <summary>
/// Implements <see cref="IJiraService"/> by delegating to <see cref="IJiraClient"/>
/// and formatting results as Discord embeds.
/// </summary>
public sealed class JiraService : IJiraService
{
    private readonly IJiraClient _client;
    private readonly ILogger<JiraService> _logger;

    /// <summary>Initialises the service.</summary>
    public JiraService(IJiraClient client, ILogger<JiraService> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Embed> SearchIssuesAsync(string projectKey, string? status, string? issueType)
    {
        _logger.LogInformation("[Discord/Jira] search_issues: project={ProjectKey} status={Status} type={IssueType}",
            projectKey, status, issueType);
        try
        {
            var result = await _client.SearchIssuesAsync(new SearchIssuesRequest
            {
                ProjectKey = projectKey,
                Status     = status,
                IssueType  = issueType,
                MaxResults = 25,
            });
            return JiraEmbedBuilder.BuildIssueList(result, projectKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search issues for project {ProjectKey}", projectKey);
            return JiraEmbedBuilder.BuildError("Jira Error", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Embed> GetIssueAsync(string issueKey)
    {
        _logger.LogInformation("[Discord/Jira] get_issue: {IssueKey}", issueKey);
        try
        {
            var issue = await _client.GetIssueAsync(issueKey);
            return JiraEmbedBuilder.BuildIssueDetail(issue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get issue {IssueKey}", issueKey);
            return JiraEmbedBuilder.BuildError("Jira Error", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Embed> CreateIssueAsync(
        string projectKey, string summary, string issueType,
        string? description, string? priority)
    {
        _logger.LogInformation("[Discord/Jira] create_issue: project={ProjectKey} type={IssueType} summary={Summary}",
            projectKey, issueType, summary);
        try
        {
            var issue = await _client.CreateIssueAsync(new CreateIssueRequest
            {
                ProjectKey  = projectKey,
                Summary     = summary,
                IssueType   = issueType,
                Description = description,
                Priority    = priority,
            });
            return JiraEmbedBuilder.BuildCreated(issue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create issue in project {ProjectKey}", projectKey);
            return JiraEmbedBuilder.BuildError("Jira Error", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Embed> AddCommentAsync(string issueKey, string comment)
    {
        _logger.LogInformation("[Discord/Jira] add_comment: {IssueKey}", issueKey);
        try
        {
            await _client.AddCommentAsync(issueKey, comment);
            return JiraEmbedBuilder.BuildCommentAdded(issueKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add comment to issue {IssueKey}", issueKey);
            return JiraEmbedBuilder.BuildError("Jira Error", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Embed> TransitionIssueAsync(string issueKey, string transitionName)
    {
        _logger.LogInformation("[Discord/Jira] transition_issue: {IssueKey} -> {Transition}", issueKey, transitionName);
        try
        {
            await _client.TransitionIssueAsync(issueKey, transitionName);
            return JiraEmbedBuilder.BuildTransitioned(issueKey, transitionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transition issue {IssueKey}", issueKey);
            return JiraEmbedBuilder.BuildError("Jira Error", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Embed> GetProjectsAsync()
    {
        _logger.LogInformation("[Discord/Jira] get_projects");
        try
        {
            var projects = await _client.GetProjectsAsync();
            return JiraEmbedBuilder.BuildProjectList(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Jira projects");
            return JiraEmbedBuilder.BuildError("Jira Error", ex.Message);
        }
    }
}
