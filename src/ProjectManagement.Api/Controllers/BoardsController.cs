using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Core.Trello;
using ProjectManagement.Core.Trello.Models;

namespace ProjectManagement.Api.Controllers;

/// <summary>Endpoints for managing Trello boards and lists.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BoardsController : ControllerBase
{
    private readonly ITrelloClient _client;
    private readonly ILogger<BoardsController> _logger;

    public BoardsController(ITrelloClient client, ILogger<BoardsController> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>Returns all Trello boards accessible by the authenticated user.</summary>
    /// <returns>A list of boards.</returns>
    /// <response code="200">Boards retrieved successfully.</response>
    /// <response code="502">Trello API returned an error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<TrelloBoard>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<List<TrelloBoard>>> GetBoards()
    {
        _logger.LogInformation("Getting all Trello boards");
        var boards = await _client.GetBoardsAsync();
        return Ok(boards);
    }

    /// <summary>Returns details of a specific Trello board.</summary>
    /// <param name="boardId">The Trello board ID.</param>
    /// <returns>The board details.</returns>
    /// <response code="200">Board found.</response>
    /// <response code="404">Board not found.</response>
    /// <response code="502">Trello API returned an error.</response>
    [HttpGet("{boardId}")]
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
    /// <param name="boardId">The Trello board ID.</param>
    /// <returns>A list of Trello lists.</returns>
    /// <response code="200">Lists retrieved successfully.</response>
    /// <response code="502">Trello API returned an error.</response>
    [HttpGet("{boardId}/lists")]
    [ProducesResponseType(typeof(List<TrelloList>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<List<TrelloList>>> GetLists(string boardId)
    {
        _logger.LogInformation("Getting lists for Trello board {BoardId}", boardId);
        var lists = await _client.GetListsAsync(boardId);
        return Ok(lists);
    }

    /// <summary>Returns all cards on a Trello board.</summary>
    /// <param name="boardId">The Trello board ID.</param>
    /// <returns>A list of cards.</returns>
    /// <response code="200">Cards retrieved successfully.</response>
    /// <response code="502">Trello API returned an error.</response>
    [HttpGet("{boardId}/cards")]
    [ProducesResponseType(typeof(List<TrelloCard>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<List<TrelloCard>>> GetCards(string boardId)
    {
        _logger.LogInformation("Getting cards for Trello board {BoardId}", boardId);
        var cards = await _client.GetCardsAsync(boardId);
        return Ok(cards);
    }
}
