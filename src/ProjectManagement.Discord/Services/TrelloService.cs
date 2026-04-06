using Discord;
using Microsoft.Extensions.Logging;
using ProjectManagement.Core.Trello;
using ProjectManagement.Core.Trello.Models;
using ProjectManagement.Discord.Formatting;

namespace ProjectManagement.Discord.Services;

/// <summary>
/// Implements <see cref="ITrelloService"/> by delegating to <see cref="ITrelloClient"/>
/// and formatting results as Discord embeds.
/// </summary>
public sealed class TrelloService : ITrelloService
{
    private readonly ITrelloClient _client;
    private readonly ILogger<TrelloService> _logger;

    /// <summary>Initialises the service.</summary>
    public TrelloService(ITrelloClient client, ILogger<TrelloService> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Embed> GetBoardsAsync()
    {
        _logger.LogInformation("[Discord/Trello] get_boards");
        try
        {
            var boards = await _client.GetBoardsAsync();
            return TrelloEmbedBuilder.BuildBoardList(boards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Trello boards");
            return TrelloEmbedBuilder.BuildError("Trello Error", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Embed> GetBoardAsync(string boardId)
    {
        _logger.LogInformation("[Discord/Trello] get_board: {BoardId}", boardId);
        try
        {
            var board = await _client.GetBoardAsync(boardId);
            return TrelloEmbedBuilder.BuildBoardDetail(board);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Trello board {BoardId}", boardId);
            return TrelloEmbedBuilder.BuildError("Trello Error", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Embed> GetCardsAsync(string boardId)
    {
        _logger.LogInformation("[Discord/Trello] get_cards: {BoardId}", boardId);
        try
        {
            var cards = await _client.GetCardsAsync(boardId);
            return TrelloEmbedBuilder.BuildCardList(cards, boardId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Trello cards for board {BoardId}", boardId);
            return TrelloEmbedBuilder.BuildError("Trello Error", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Embed> GetCardAsync(string cardId)
    {
        _logger.LogInformation("[Discord/Trello] get_card: {CardId}", cardId);
        try
        {
            var card = await _client.GetCardAsync(cardId);
            return TrelloEmbedBuilder.BuildCardDetail(card);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Trello card {CardId}", cardId);
            return TrelloEmbedBuilder.BuildError("Trello Error", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Embed> CreateCardAsync(string listId, string name, string? description)
    {
        _logger.LogInformation("[Discord/Trello] create_card: list={ListId} name={Name}", listId, name);
        try
        {
            var card = await _client.CreateCardAsync(new CreateCardRequest
            {
                IdList = listId,
                Name   = name,
                Desc   = description,
            });
            return TrelloEmbedBuilder.BuildCardCreated(card);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Trello card in list {ListId}", listId);
            return TrelloEmbedBuilder.BuildError("Trello Error", ex.Message);
        }
    }
}
