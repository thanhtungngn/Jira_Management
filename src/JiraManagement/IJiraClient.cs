using JiraManagement.Models;

namespace JiraManagement;

public interface IJiraClient
{
    Task<JiraUser> GetCurrentUserAsync();
    Task<List<JiraProject>> GetProjectsAsync();
    Task<JiraProject> GetProjectAsync(string projectKey);
    Task<SearchResult> SearchIssuesAsync(SearchIssuesRequest request);
    Task<JiraIssue> GetIssueAsync(string issueKey);
    Task<JiraIssue> CreateIssueAsync(CreateIssueRequest request);
    Task UpdateIssueAsync(string issueKey, UpdateIssueRequest request);
    Task AddCommentAsync(string issueKey, string comment);
    Task TransitionIssueAsync(string issueKey, string transitionName);
}
