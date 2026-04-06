using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectManagement.Core.Trello.Models;

namespace ProjectManagement.Core.Trello;

public class TrelloClient : ITrelloClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TrelloClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public TrelloClient(HttpClient httpClient, ILogger<TrelloClient>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger ?? NullLogger<TrelloClient>.Instance;
    }

    public async Task<List<TrelloBoard>> GetBoardsAsync()
    {
        _logger.LogDebug("Fetching Trello boards");
        var response = await _httpClient.GetAsync("members/me/boards?fields=id,name,desc,closed,url");
        await EnsureSuccessAsync(response);
        var boards = await response.Content.ReadFromJsonAsync<List<TrelloBoard>>(JsonOptions) ?? [];
        _logger.LogInformation("Retrieved {Count} boards", boards.Count);
        return boards;
    }

    public async Task<TrelloBoard> GetBoardAsync(string boardId)
    {
        _logger.LogDebug("Fetching board {BoardId}", boardId);
        var response = await _httpClient.GetAsync(
            $"boards/{Uri.EscapeDataString(boardId)}?fields=id,name,desc,closed,url");
        await EnsureSuccessAsync(response);
        var board = await response.Content.ReadFromJsonAsync<TrelloBoard>(JsonOptions)
               ?? throw new InvalidOperationException("Empty response from Trello.");
        _logger.LogInformation("Retrieved board {BoardName}", board.Name);
        return board;
    }

    public async Task<List<TrelloList>> GetListsAsync(string boardId)
    {
        _logger.LogDebug("Fetching lists for board {BoardId}", boardId);
        var response = await _httpClient.GetAsync(
            $"boards/{Uri.EscapeDataString(boardId)}/lists?fields=id,name,closed,idBoard");
        await EnsureSuccessAsync(response);
        var lists = await response.Content.ReadFromJsonAsync<List<TrelloList>>(JsonOptions) ?? [];
        _logger.LogInformation("Retrieved {Count} lists for board {BoardId}", lists.Count, boardId);
        return lists;
    }

    public async Task<List<TrelloCard>> GetCardsAsync(string boardId)
    {
        _logger.LogDebug("Fetching cards for board {BoardId}", boardId);
        var response = await _httpClient.GetAsync(
            $"boards/{Uri.EscapeDataString(boardId)}/cards?fields=id,name,desc,closed,idBoard,idList,url,due,labels");
        await EnsureSuccessAsync(response);
        var cards = await response.Content.ReadFromJsonAsync<List<TrelloCard>>(JsonOptions) ?? [];
        _logger.LogInformation("Retrieved {Count} cards for board {BoardId}", cards.Count, boardId);
        return cards;
    }

    public async Task<TrelloCard> GetCardAsync(string cardId)
    {
        _logger.LogDebug("Fetching card {CardId}", cardId);
        var response = await _httpClient.GetAsync(
            $"cards/{Uri.EscapeDataString(cardId)}?fields=id,name,desc,closed,idBoard,idList,url,due,labels");
        await EnsureSuccessAsync(response);
        var card = await response.Content.ReadFromJsonAsync<TrelloCard>(JsonOptions)
               ?? throw new InvalidOperationException("Empty response from Trello.");
        _logger.LogInformation("Retrieved card {CardName}", card.Name);
        return card;
    }

    public async Task<TrelloCard> CreateCardAsync(CreateCardRequest request)
    {
        _logger.LogDebug("Creating card '{Name}' in list {ListId}", request.Name, request.IdList);
        var body = new Dictionary<string, object> { ["idList"] = request.IdList, ["name"] = request.Name };
        if (request.Desc is not null) body["desc"] = request.Desc;
        if (request.Due is not null) body["due"] = request.Due.Value.ToString("o");

        var response = await _httpClient.PostAsJsonAsync("cards", body, JsonOptions);
        await EnsureSuccessAsync(response);
        var card = await response.Content.ReadFromJsonAsync<TrelloCard>(JsonOptions)
               ?? throw new InvalidOperationException("Empty response from Trello.");
        _logger.LogInformation("Created card {CardId} '{CardName}'", card.Id, card.Name);
        return card;
    }

    public async Task<TrelloCard> UpdateCardAsync(string cardId, UpdateCardRequest request)
    {
        _logger.LogDebug("Updating card {CardId}", cardId);
        var body = new Dictionary<string, object>();
        if (request.Name is not null) body["name"] = request.Name;
        if (request.Desc is not null) body["desc"] = request.Desc;
        if (request.IdList is not null) body["idList"] = request.IdList;
        if (request.Due is not null) body["due"] = request.Due.Value.ToString("o");
        if (request.Closed is not null) body["closed"] = request.Closed.Value;

        var response = await _httpClient.PutAsJsonAsync(
            $"cards/{Uri.EscapeDataString(cardId)}", body, JsonOptions);
        await EnsureSuccessAsync(response);
        var card = await response.Content.ReadFromJsonAsync<TrelloCard>(JsonOptions)
               ?? throw new InvalidOperationException("Empty response from Trello.");
        _logger.LogInformation("Updated card {CardId}", cardId);
        return card;
    }

    public async Task DeleteCardAsync(string cardId)
    {
        _logger.LogDebug("Deleting card {CardId}", cardId);
        var response = await _httpClient.DeleteAsync($"cards/{Uri.EscapeDataString(cardId)}");
        await EnsureSuccessAsync(response);
        _logger.LogInformation("Deleted card {CardId}", cardId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Trello API error {(int)response.StatusCode} ({response.ReasonPhrase}): {body}");
        }
    }
}
