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

    /// <summary>Gets details of a Confluence page by page ID.</summary>
    /// <param name="pageId">The Confluence page ID.</param>
    /// <returns>The Confluence page metadata and content.</returns>
    [HttpGet("{pageId}")]
    [ProducesResponseType(typeof(ConfluencePage), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ConfluencePage>> GetPage(string pageId)
    {
        _logger.LogInformation("Getting Confluence page {PageId}", pageId);
        var page = await _client.GetPageAsync(pageId);
        return Ok(page);
    }

    /// <summary>Gets child pages under a Confluence page to inspect structure.</summary>
    /// <param name="pageId">The parent Confluence page ID.</param>
    /// <param name="limit">Maximum number of child pages to return (default 50).</param>
    /// <returns>A list of child pages.</returns>
    [HttpGet("{pageId}/children")]
    [ProducesResponseType(typeof(List<ConfluencePage>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<List<ConfluencePage>>> GetChildren(string pageId, [FromQuery] int limit = 50)
    {
        _logger.LogInformation("Getting Confluence children for {PageId}", pageId);
        var pages = await _client.GetChildrenAsync(pageId, limit);
        return Ok(pages);
    }

    /// <summary>Creates a new Confluence page in a space, optionally under a parent page.</summary>
    /// <param name="request">Page creation payload.</param>
    /// <returns>The newly created page metadata.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ConfluencePage), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ConfluencePage>> CreatePage([FromBody] CreateConfluencePageRequest request)
    {
        _logger.LogInformation("Creating Confluence page '{Title}' in space {SpaceKey}", request.Title, request.SpaceKey);
        var page = await _client.CreatePageAsync(request.SpaceKey, request.Title, request.Content, request.ParentPageId);
        return CreatedAtAction(nameof(GetPage), new { pageId = page.Id }, page);
    }

    /// <summary>Creates a folder-like Confluence container page.</summary>
    /// <param name="request">Folder creation payload.</param>
    /// <returns>The newly created folder page metadata.</returns>
    [HttpPost("folders")]
    [ProducesResponseType(typeof(ConfluencePage), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ConfluencePage>> CreateFolder([FromBody] CreateConfluenceFolderRequest request)
    {
        _logger.LogInformation("Creating Confluence folder '{Title}' in space {SpaceKey}", request.Title, request.SpaceKey);
        var page = await _client.CreateFolderAsync(request.SpaceKey, request.Title, request.ParentPageId);
        return CreatedAtAction(nameof(GetPage), new { pageId = page.Id }, page);
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

    /// <summary>Moves a Confluence page under a new parent page.</summary>
    /// <param name="pageId">The Confluence page ID to move.</param>
    /// <param name="request">Target parent page ID and edit mode.</param>
    /// <returns>The updated page metadata after move.</returns>
    [HttpPut("{pageId}/move")]
    [ProducesResponseType(typeof(ConfluencePage), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ConfluencePage>> MovePage(string pageId, [FromBody] MoveConfluencePageRequest request)
    {
        _logger.LogInformation("Moving Confluence page {PageId} to parent {ParentPageId}", pageId, request.NewParentPageId);
        var page = await _client.MovePageAsync(pageId, request.NewParentPageId, request.MinorEdit);
        return Ok(page);
    }

    /// <summary>Deletes a Confluence page.</summary>
    /// <param name="pageId">The Confluence page ID.</param>
    [HttpDelete("{pageId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> DeletePage(string pageId)
    {
        _logger.LogInformation("Deleting Confluence page {PageId}", pageId);
        await _client.DeletePageAsync(pageId);
        return NoContent();
    }
}

public class CreateConfluencePageRequest
{
    [Required]
    public string SpaceKey { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? ParentPageId { get; set; }
}

public class CreateConfluenceFolderRequest
{
    [Required]
    public string SpaceKey { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? ParentPageId { get; set; }
}

public class UpdateConfluencePageRequest
{
    [Required]
    public string Content { get; set; } = string.Empty;

    public string? Title { get; set; }

    public bool MinorEdit { get; set; }
}

public class MoveConfluencePageRequest
{
    [Required]
    public string NewParentPageId { get; set; } = string.Empty;

    public bool MinorEdit { get; set; }
}
