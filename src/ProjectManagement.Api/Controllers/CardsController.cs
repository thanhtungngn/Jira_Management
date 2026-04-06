using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Core.Trello;
using ProjectManagement.Core.Trello.Models;

namespace ProjectManagement.Api.Controllers;

/// <summary>Endpoints for managing Trello cards.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CardsController : ControllerBase
{
    private readonly ITrelloClient _client;
    private readonly ILogger<CardsController> _logger;

    public CardsController(ITrelloClient client, ILogger<CardsController> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>Returns details of a specific Trello card.</summary>
    /// <param name="cardId">The Trello card ID.</param>
    /// <returns>The card details.</returns>
    /// <response code="200">Card found.</response>
    /// <response code="404">Card not found.</response>
    /// <response code="502">Trello API returned an error.</response>
    [HttpGet("{cardId}")]
    [ProducesResponseType(typeof(TrelloCard), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<TrelloCard>> GetCard(string cardId)
    {
        _logger.LogInformation("Getting Trello card {CardId}", cardId);
        var card = await _client.GetCardAsync(cardId);
        return Ok(card);
    }

    /// <summary>Creates a new Trello card on a list.</summary>
    /// <param name="request">Card creation details. <c>idList</c> and <c>name</c> are required.</param>
    /// <returns>The newly created card.</returns>
    /// <response code="201">Card created successfully.</response>
    /// <response code="400">Request body is missing or invalid.</response>
    /// <response code="502">Trello API returned an error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TrelloCard), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<TrelloCard>> CreateCard([FromBody] CreateCardRequest request)
    {
        _logger.LogInformation("Creating card '{Name}' in list {ListId}", request.Name, request.IdList);
        var card = await _client.CreateCardAsync(request);
        return CreatedAtAction(nameof(GetCard), new { cardId = card.Id }, card);
    }

    /// <summary>Updates a Trello card. All fields are optional — omit to leave unchanged.</summary>
    /// <param name="cardId">The Trello card ID.</param>
    /// <param name="request">Fields to update.</param>
    /// <returns>The updated card.</returns>
    /// <response code="200">Card updated successfully.</response>
    /// <response code="404">Card not found.</response>
    /// <response code="502">Trello API returned an error.</response>
    [HttpPut("{cardId}")]
    [ProducesResponseType(typeof(TrelloCard), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<TrelloCard>> UpdateCard(string cardId, [FromBody] UpdateCardRequest request)
    {
        _logger.LogInformation("Updating Trello card {CardId}", cardId);
        var card = await _client.UpdateCardAsync(cardId, request);
        return Ok(card);
    }

    /// <summary>Permanently deletes a Trello card.</summary>
    /// <param name="cardId">The Trello card ID.</param>
    /// <response code="204">Card deleted successfully.</response>
    /// <response code="404">Card not found.</response>
    /// <response code="502">Trello API returned an error.</response>
    [HttpDelete("{cardId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> DeleteCard(string cardId)
    {
        _logger.LogInformation("Deleting Trello card {CardId}", cardId);
        await _client.DeleteCardAsync(cardId);
        return NoContent();
    }
}
