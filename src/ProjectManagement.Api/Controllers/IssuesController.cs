using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Core.Jira;
using ProjectManagement.Core.Jira.Models;

namespace ProjectManagement.Api.Controllers;

/// <summary>Endpoints for managing Jira issues.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class IssuesController : ControllerBase
{
    private readonly IJiraClient _client;
    private readonly ILogger<IssuesController> _logger;

    public IssuesController(IJiraClient client, ILogger<IssuesController> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>Searches for issues in a project using optional filters.</summary>
    /// <param name="request">
    ///   Query parameters:<br/>
    ///   <c>projectKey</c> — required Jira project key.<br/>
    ///   <c>status</c> — filter by status name (e.g. <c>In Progress</c>).<br/>
    ///   <c>issueType</c> — filter by issue type (e.g. <c>Bug</c>, <c>Story</c>).<br/>
    ///   <c>assigneeEmail</c> — filter by assignee email address.<br/>
    ///   <c>maxResults</c> — page size, default 50.<br/>
    ///   <c>nextPageToken</c> — opaque token from a previous response for pagination; omit for the first page.
    /// </param>
    /// <returns>A paginated list of matching issues.</returns>
    /// <response code="200">Issues retrieved successfully.</response>
    /// <response code="502">Jira API returned an error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<SearchResult>> SearchIssues([FromQuery] SearchIssuesRequest request)
    {
        _logger.LogInformation("Searching issues in project {ProjectKey}", request.ProjectKey);
        var result = await _client.SearchIssuesAsync(request);
        return Ok(result);
    }

    /// <summary>Returns the full details of a single issue, including recent comments.</summary>
    /// <param name="key">The issue key (e.g. <c>MYPROJ-42</c>).</param>
    /// <returns>The issue with all fields and comments.</returns>
    /// <response code="200">Issue found.</response>
    /// <response code="404">Issue not found.</response>
    /// <response code="502">Jira API returned an error.</response>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(JiraIssue), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<JiraIssue>> GetIssue(string key)
    {
        _logger.LogInformation("Getting issue {IssueKey}", key);
        var issue = await _client.GetIssueAsync(key);
        return Ok(issue);
    }

    /// <summary>Creates a new issue in a Jira project.</summary>
    /// <param name="request">
    ///   Issue details. <c>projectKey</c> and <c>summary</c> are required.
    ///   <c>issueType</c> defaults to <c>Task</c> when omitted.
    /// </param>
    /// <returns>The newly created issue.</returns>
    /// <response code="201">Issue created successfully.</response>
    /// <response code="400">Request body is missing or invalid.</response>
    /// <response code="502">Jira API returned an error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(JiraIssue), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<JiraIssue>> CreateIssue([FromBody] CreateIssueRequest request)
    {
        _logger.LogInformation("Creating issue in project {ProjectKey}: {Summary}", request.ProjectKey, request.Summary);
        var issue = await _client.CreateIssueAsync(request);
        return CreatedAtAction(nameof(GetIssue), new { key = issue.Key }, issue);
    }

    /// <summary>Updates the editable fields of an existing issue. All fields are optional.</summary>
    /// <param name="key">The issue key (e.g. <c>MYPROJ-42</c>).</param>
    /// <param name="request">Fields to update. Omit a field to leave it unchanged.</param>
    /// <response code="204">Issue updated successfully.</response>
    /// <response code="404">Issue not found.</response>
    /// <response code="502">Jira API returned an error.</response>
    [HttpPut("{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> UpdateIssue(string key, [FromBody] UpdateIssueRequest request)
    {
        _logger.LogInformation("Updating issue {IssueKey}", key);
        await _client.UpdateIssueAsync(key, request);
        return NoContent();
    }

    /// <summary>Transitions an issue to a new workflow status.</summary>
    /// <param name="key">The issue key (e.g. <c>MYPROJ-42</c>).</param>
    /// <param name="request">
    ///   The target transition name exactly as it appears in Jira
    ///   (e.g. <c>In Progress</c>, <c>Done</c>). Name matching is case-insensitive.
    /// </param>
    /// <response code="204">Issue transitioned successfully.</response>
    /// <response code="404">Issue not found or transition name not available.</response>
    /// <response code="502">Jira API returned an error.</response>
    [HttpPost("{key}/transitions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> TransitionIssue(string key, [FromBody] TransitionRequest request)
    {
        _logger.LogInformation("Transitioning issue {IssueKey} to '{TransitionName}'", key, request.TransitionName);
        await _client.TransitionIssueAsync(key, request.TransitionName);
        return NoContent();
    }

    /// <summary>Adds a plain-text comment to an issue.</summary>
    /// <param name="key">The issue key (e.g. <c>MYPROJ-42</c>).</param>
    /// <param name="request">The comment text.</param>
    /// <response code="204">Comment added successfully.</response>
    /// <response code="404">Issue not found.</response>
    /// <response code="502">Jira API returned an error.</response>
    [HttpPost("{key}/comments")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> AddComment(string key, [FromBody] AddCommentRequest request)
    {
        _logger.LogInformation("Adding comment to issue {IssueKey}", key);
        await _client.AddCommentAsync(key, request.Text);
        return NoContent();
    }
}
