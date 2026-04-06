using ProjectManagement.Core.Trello.Models;

namespace ProjectManagement.Core.Trello;

public interface ITrelloClient
{
    Task<List<TrelloBoard>> GetBoardsAsync();
    Task<TrelloBoard> GetBoardAsync(string boardId);
    Task<List<TrelloList>> GetListsAsync(string boardId);
    Task<List<TrelloCard>> GetCardsAsync(string boardId);
    Task<TrelloCard> GetCardAsync(string cardId);
    Task<TrelloCard> CreateCardAsync(CreateCardRequest request);
    Task<TrelloCard> UpdateCardAsync(string cardId, UpdateCardRequest request);
    Task DeleteCardAsync(string cardId);
}
