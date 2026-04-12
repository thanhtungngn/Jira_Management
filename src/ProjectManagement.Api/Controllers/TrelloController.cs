using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Core.Trello;
using ProjectManagement.Core.Trello.Models;

namespace ProjectManagement.Api.Controllers;

/// <summary>Endpoints for Trello boards, lists, and cards.</summary>
[ApiController]
[Route("api/trello")]
[Produces("application/json")]
public class TrelloController : ControllerBase
{
    private readonly ITrelloClient _client;
    private readonly ILogger<TrelloController> _logger;

    public TrelloController(ITrelloClient client, ILogger<TrelloController> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>Returns all Trello boards accessible by the authenticated user.</summary>
    [HttpGet("boards")]
    [HttpGet("/api/boards")]
    [ProducesResponseType(typeof(List<TrelloBoard>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<List<TrelloBoard>>> GetBoards()
    {
        _logger.LogInformation("Getting all Trello boards");
        var boards = await _client.GetBoardsAsync();
        return Ok(boards);
    }

    /// <summary>Returns details of a specific Trello board.</summary>
    [HttpGet("boards/{boardId}")]
    [HttpGet("/api/boards/{boardId}")]
    [ProducesResponseType(typeof(TrelloBoard), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<TrelloBoard>> GetBoard(string boardId)
    {
        _logger.LogInformation("Getting Trello board {BoardId}", boardId);
        var board = await _client.GetBoardAsync(boardId);
        return Ok(board);
    }

    /// <summary>Returns all lists on a Trello board.</summary>
    [HttpGet("boards/{boardId}/lists")]
    [HttpGet("/api/boards/{boardId}/lists")]
    [ProducesResponseType(typeof(List<TrelloList>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<List<TrelloList>>> GetLists(string boardId)
    {
        _logger.LogInformation("Getting lists for Trello board {BoardId}", boardId);
        var lists = await _client.GetListsAsync(boardId);
        return Ok(lists);
    }

    /// <summary>Returns all cards on a Trello board.</summary>
    [HttpGet("boards/{boardId}/cards")]
    [HttpGet("/api/boards/{boardId}/cards")]
    [ProducesResponseType(typeof(List<TrelloCard>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<List<TrelloCard>>> GetCards(string boardId)
    {
        _logger.LogInformation("Getting cards for Trello board {BoardId}", boardId);
        var cards = await _client.GetCardsAsync(boardId);
        return Ok(cards);
    }

    /// <summary>Returns details of a specific Trello card.</summary>
    [HttpGet("cards/{cardId}")]
    [HttpGet("/api/cards/{cardId}")]
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
    [HttpPost("cards")]
    [HttpPost("/api/cards")]
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
    [HttpPut("cards/{cardId}")]
    [HttpPut("/api/cards/{cardId}")]
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
    [HttpDelete("cards/{cardId}")]
    [HttpDelete("/api/cards/{cardId}")]
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
