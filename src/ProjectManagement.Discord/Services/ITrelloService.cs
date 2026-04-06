using Discord;

namespace ProjectManagement.Discord.Services;

/// <summary>
/// Provides business-logic operations for Trello slash commands.
/// Returns Discord <see cref="Embed"/> objects ready to be sent as bot replies.
/// </summary>
public interface ITrelloService
{
    /// <summary>List all Trello boards and return a formatted embed.</summary>
    Task<Embed> GetBoardsAsync();

    /// <summary>Get a single board and return a formatted embed.</summary>
    Task<Embed> GetBoardAsync(string boardId);

    /// <summary>List cards on a board and return a formatted embed.</summary>
    Task<Embed> GetCardsAsync(string boardId);

    /// <summary>Get a single card and return a formatted embed.</summary>
    Task<Embed> GetCardAsync(string cardId);

    /// <summary>Create a new card and return a confirmation embed.</summary>
    Task<Embed> CreateCardAsync(string listId, string name, string? description);
}
