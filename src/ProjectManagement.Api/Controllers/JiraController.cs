using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Core.Jira;
using ProjectManagement.Core.Jira.Models;

namespace ProjectManagement.Api.Controllers;

/// <summary>Endpoints for Jira projects and issues.</summary>
[ApiController]
[Route("api/jira")]
[Produces("application/json")]
public class JiraController : ControllerBase
{
    private readonly IJiraClient _client;
    private readonly ILogger<JiraController> _logger;

    public JiraController(IJiraClient client, ILogger<JiraController> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>Returns all accessible Jira projects (up to 50).</summary>
    [HttpGet("projects")]
    [HttpGet("/api/projects")]
    [ProducesResponseType(typeof(List<JiraProject>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<List<JiraProject>>> GetProjects()
    {
        _logger.LogInformation("Getting all Jira projects");
        var projects = await _client.GetProjectsAsync();
        return Ok(projects);
    }

    /// <summary>Returns details of a specific Jira project.</summary>
    [HttpGet("projects/{key}")]
    [HttpGet("/api/projects/{key}")]
    [ProducesResponseType(typeof(JiraProject), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<JiraProject>> GetProject(string key)
    {
        _logger.LogInformation("Getting project {ProjectKey}", key);
        var project = await _client.GetProjectAsync(key);
        return Ok(project);
    }

    /// <summary>Searches for issues in a project using optional filters.</summary>
    [HttpGet("issues")]
    [HttpGet("/api/issues")]
    [ProducesResponseType(typeof(SearchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<SearchResult>> SearchIssues([FromQuery] SearchIssuesRequest request)
    {
        _logger.LogInformation("Searching issues in project {ProjectKey}", request.ProjectKey);
        var result = await _client.SearchIssuesAsync(request);
        return Ok(result);
    }

    /// <summary>Returns the full details of a single issue, including recent comments.</summary>
    [HttpGet("issues/{key}")]
    [HttpGet("/api/issues/{key}")]
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
    [HttpPost("issues")]
    [HttpPost("/api/issues")]
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
    [HttpPut("issues/{key}")]
    [HttpPut("/api/issues/{key}")]
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
    [HttpPost("issues/{key}/transitions")]
    [HttpPost("/api/issues/{key}/transitions")]
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
    [HttpPost("issues/{key}/comments")]
    [HttpPost("/api/issues/{key}/comments")]
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
