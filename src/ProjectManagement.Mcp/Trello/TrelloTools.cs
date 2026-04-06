using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using ProjectManagement.Core.Trello;
using ProjectManagement.Core.Trello.Models;

namespace ProjectManagement.Mcp.Trello;

[McpServerToolType]
public sealed class TrelloTools
{
    private readonly ITrelloClient _client;
    private readonly ILogger<TrelloTools> _logger;

    public TrelloTools(ITrelloClient client, ILogger<TrelloTools> logger)
    {
        _client = client;
        _logger = logger;
    }

    [McpServerTool(Name = "get_boards"), Description("List all Trello boards accessible by the authenticated user.")]
    public async Task<List<TrelloBoard>> GetBoardsAsync()
    {
        _logger.LogInformation("[MCP] get_boards");
        return await _client.GetBoardsAsync();
    }

    [McpServerTool(Name = "get_board"), Description("Get details of a specific Trello board by its ID.")]
    public async Task<TrelloBoard> GetBoardAsync(string boardId)
    {
        _logger.LogInformation("[MCP] get_board: {BoardId}", boardId);
        return await _client.GetBoardAsync(boardId);
    }

    [McpServerTool(Name = "get_lists"), Description("Get all lists on a Trello board.")]
    public async Task<List<TrelloList>> GetListsAsync(string boardId)
    {
        _logger.LogInformation("[MCP] get_lists: {BoardId}", boardId);
        return await _client.GetListsAsync(boardId);
    }

    [McpServerTool(Name = "get_cards"), Description("Get all cards on a Trello board.")]
    public async Task<List<TrelloCard>> GetCardsAsync(string boardId)
    {
        _logger.LogInformation("[MCP] get_cards: {BoardId}", boardId);
        return await _client.GetCardsAsync(boardId);
    }

    [McpServerTool(Name = "get_card"), Description("Get details of a specific Trello card by its ID.")]
    public async Task<TrelloCard> GetCardAsync(string cardId)
    {
        _logger.LogInformation("[MCP] get_card: {CardId}", cardId);
        return await _client.GetCardAsync(cardId);
    }

    [McpServerTool(Name = "create_card"), Description("Create a new card on a Trello list.")]
    public async Task<TrelloCard> CreateCardAsync(
        string idList,
        string name,
        string? desc = null,
        DateTime? due = null)
    {
        _logger.LogInformation("[MCP] create_card: list={ListId} name={Name}", idList, name);
        return await _client.CreateCardAsync(new CreateCardRequest
        {
            IdList = idList,
            Name   = name,
            Desc   = desc,
            Due    = due,
        });
    }

    [McpServerTool(Name = "update_card"), Description("Update a Trello card's name, description, list, due date, or archived state.")]
    public async Task<TrelloCard> UpdateCardAsync(
        string cardId,
        string? name = null,
        string? desc = null,
        string? idList = null,
        DateTime? due = null,
        bool? closed = null)
    {
        _logger.LogInformation("[MCP] update_card: {CardId}", cardId);
        return await _client.UpdateCardAsync(cardId, new UpdateCardRequest
        {
            Name   = name,
            Desc   = desc,
            IdList = idList,
            Due    = due,
            Closed = closed,
        });
    }

    [McpServerTool(Name = "delete_card"), Description("Permanently delete a Trello card.")]
    public async Task DeleteCardAsync(string cardId)
    {
        _logger.LogInformation("[MCP] delete_card: {CardId}", cardId);
        await _client.DeleteCardAsync(cardId);
    }
}
