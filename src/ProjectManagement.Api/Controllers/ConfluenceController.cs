using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Core.Confluence;
using ProjectManagement.Core.Confluence.Models;

namespace ProjectManagement.Api.Controllers;

/// <summary>Endpoints for updating Confluence pages.</summary>
[ApiController]
[Route("api/confluence/pages")]
[Produces("application/json")]
public class ConfluenceController : ControllerBase
{
    private readonly IConfluenceClient _client;
    private readonly ILogger<ConfluenceController> _logger;

    public ConfluenceController(IConfluenceClient client, ILogger<ConfluenceController> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>Updates an existing Confluence page using storage-format content.</summary>
    /// <param name="pageId">The Confluence page ID.</param>
    /// <param name="request">Update content and optional metadata.</param>
    /// <returns>The updated Confluence page metadata.</returns>
    /// <response code="200">Page updated successfully.</response>
    /// <response code="400">Request body is missing or invalid.</response>
    /// <response code="502">Confluence API returned an error.</response>
    [HttpPut("{pageId}")]
    [ProducesResponseType(typeof(ConfluencePage), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ConfluencePage>> UpdatePage(string pageId, [FromBody] UpdateConfluencePageRequest request)
    {
        _logger.LogInformation("Updating Confluence page {PageId}", pageId);
        var page = await _client.UpdatePageAsync(pageId, request.Content, request.Title, request.MinorEdit);
        return Ok(page);
    }
}

public class UpdateConfluencePageRequest
{
    [Required]
    public string Content { get; set; } = string.Empty;

    public string? Title { get; set; }

    public bool MinorEdit { get; set; }
}
