using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Core.Jira;
using ProjectManagement.Core.Jira.Models;

namespace ProjectManagement.Api.Controllers;

/// <summary>Verifies connectivity and authentication with the configured Jira instance.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly IJiraClient _client;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IJiraClient client, ILogger<HealthController> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>Pings the Jira API and returns the authenticated user's details.</summary>
    /// <remarks>
    /// Calls <c>GET /rest/api/3/myself</c> on the configured Jira instance.
    /// A <c>200 OK</c> response means credentials are valid and the instance is reachable.
    /// </remarks>
    /// <returns>Connection status and the authenticated user's account information.</returns>
    /// <response code="200">Connection successful — credentials are valid.</response>
    /// <response code="502">Could not reach Jira or credentials were rejected.</response>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<HealthResponse>> CheckAsync()
    {
        var user = await _client.GetCurrentUserAsync();
        return Ok(new HealthResponse
        {
            Status      = "ok",
            DisplayName = user.DisplayName,
            Email       = user.EmailAddress,
            AccountId   = user.AccountId,
        });
    }
}

/// <summary>Result returned by the health check endpoint.</summary>
public class HealthResponse
{
    /// <summary>Always <c>"ok"</c> when the request succeeds.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Display name of the authenticated Jira user.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Email address of the authenticated Jira user.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Jira account ID of the authenticated user.</summary>
    public string AccountId { get; init; } = string.Empty;
}
