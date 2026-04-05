using ProjectManagement.Core.GitHub.Models;

namespace ProjectManagement.Core.GitHub;

public interface IGitHubClient
{
    Task<List<GitHubRepository>> ListRepositoriesAsync();
    Task<GitHubRepository> GetRepositoryAsync(string owner, string repo);
    Task<List<GitHubBranch>> ListBranchesAsync(string owner, string repo);
    Task<List<GitHubCommit>> ListCommitsAsync(ListCommitsRequest request);
    Task<List<GitHubIssue>> ListIssuesAsync(string owner, string repo, string state = "open");
    Task<GitHubIssue> GetIssueAsync(string owner, string repo, int issueNumber);
    Task<GitHubIssue> CreateIssueAsync(string owner, string repo, CreateIssueRequest request);
}
