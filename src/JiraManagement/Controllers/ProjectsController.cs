using JiraManagement.Models;
using Microsoft.AspNetCore.Mvc;

namespace JiraManagement.Controllers;

/// <summary>Endpoints for browsing Jira projects.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IJiraClient _client;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IJiraClient client, ILogger<ProjectsController> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>Returns all accessible Jira projects (up to 50).</summary>
    /// <returns>A list of projects.</returns>
    /// <response code="200">Projects retrieved successfully.</response>
    /// <response code="502">Jira API returned an error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<JiraProject>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<List<JiraProject>>> GetProjects()
    {
        _logger.LogInformation("Getting all Jira projects");
        var projects = await _client.GetProjectsAsync();
        return Ok(projects);
    }

    /// <summary>Returns details of a specific Jira project.</summary>
    /// <param name="key">The project key (e.g. <c>MYPROJ</c>).</param>
    /// <returns>The project details.</returns>
    /// <response code="200">Project found.</response>
    /// <response code="404">Project not found.</response>
    /// <response code="502">Jira API returned an error.</response>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(JiraProject), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<JiraProject>> GetProject(string key)
    {
        _logger.LogInformation("Getting project {ProjectKey}", key);
        var project = await _client.GetProjectAsync(key);
        return Ok(project);
    }
}
