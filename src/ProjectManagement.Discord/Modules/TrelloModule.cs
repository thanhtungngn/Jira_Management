using System.Diagnostics.CodeAnalysis;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using ProjectManagement.Discord.Services;

namespace ProjectManagement.Discord.Modules;

/// <summary>
/// Slash-command module that exposes Trello operations under the <c>/trello</c> group.
/// Each command delegates business logic to <see cref="ITrelloService"/>.
/// </summary>
/// <remarks>
/// This class is a thin Discord.Net integration wrapper.
/// All testable business logic lives in <see cref="ITrelloService"/> and its implementation.
/// </remarks>
[ExcludeFromCodeCoverage]
[Group("trello", "Trello board management commands")]
public sealed class TrelloModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ITrelloService _service;
    private readonly ILogger<TrelloModule> _logger;

    /// <summary>Initialises the module.</summary>
    public TrelloModule(ITrelloService service, ILogger<TrelloModule> logger)
    {
        _service = service;
        _logger  = logger;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>List all Trello boards accessible to the configured token.</summary>
    [SlashCommand("boards", "List your Trello boards")]
    public async Task BoardsAsync()
    {
        await DeferAsync();
        _logger.LogDebug("Trello boards");
        var embed = await _service.GetBoardsAsync();
        await FollowupAsync(embed: embed);
    }

    /// <summary>Get details for a specific Trello board.</summary>
    [SlashCommand("board", "Get details of a Trello board")]
    public async Task BoardAsync(
        [Summary("board_id", "The Trello board ID")] string boardId)
    {
        await DeferAsync();
        _logger.LogDebug("Trello board: {BoardId}", boardId);
        var embed = await _service.GetBoardAsync(boardId);
        await FollowupAsync(embed: embed);
    }

    /// <summary>List all cards on a Trello board.</summary>
    [SlashCommand("cards", "List cards on a Trello board")]
    public async Task CardsAsync(
        [Summary("board_id", "The Trello board ID")] string boardId)
    {
        await DeferAsync();
        _logger.LogDebug("Trello cards: board={BoardId}", boardId);
        var embed = await _service.GetCardsAsync(boardId);
        await FollowupAsync(embed: embed);
    }

    /// <summary>Get details for a specific Trello card.</summary>
    [SlashCommand("card", "Get details of a Trello card")]
    public async Task CardAsync(
        [Summary("card_id", "The Trello card ID")] string cardId)
    {
        await DeferAsync();
        _logger.LogDebug("Trello card: {CardId}", cardId);
        var embed = await _service.GetCardAsync(cardId);
        await FollowupAsync(embed: embed);
    }

    /// <summary>Create a new card in a Trello list.</summary>
    [SlashCommand("create-card", "Create a new card in a Trello list")]
    public async Task CreateCardAsync(
        [Summary("list_id",     "The Trello list ID to add the card to")] string listId,
        [Summary("name",        "Card title / name")]                      string name,
        [Summary("description", "Optional description")]                   string? description = null)
    {
        await DeferAsync();
        _logger.LogDebug("Trello create-card: list={ListId} name={Name}", listId, name);
        var embed = await _service.CreateCardAsync(listId, name, description);
        await FollowupAsync(embed: embed);
    }
}
