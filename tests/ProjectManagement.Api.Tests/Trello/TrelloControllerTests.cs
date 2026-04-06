using System.Net;
using System.Net.Http.Json;
using Moq;
using ProjectManagement.Core.Trello;
using ProjectManagement.Core.Trello.Models;

namespace ProjectManagement.Api.Tests.Trello;

public class TrelloControllerTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _http;

    public TrelloControllerTests(ApiTestFactory factory)
    {
        _factory = factory;
        _http    = factory.CreateClient();
    }

    // ── GET /api/boards ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetBoards_Returns200_WithBoardList()
    {
        _factory.TrelloMock.Setup(c => c.GetBoardsAsync())
            .ReturnsAsync([new TrelloBoard { Id = "b1", Name = "Sprint Board" }]);

        var response = await _http.GetAsync("/api/boards");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var boards = await response.Content.ReadFromJsonAsync<List<TrelloBoard>>();
        Assert.Single(boards!);
        Assert.Equal("Sprint Board", boards![0].Name);
    }

    [Fact]
    public async Task GetBoards_Returns502_WhenClientThrows()
    {
        _factory.TrelloMock.Setup(c => c.GetBoardsAsync())
            .ThrowsAsync(new HttpRequestException("Unauthorized"));

        var response = await _http.GetAsync("/api/boards");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    // ── GET /api/boards/{boardId} ─────────────────────────────────────────────

    [Fact]
    public async Task GetBoard_Returns200_WithBoardDetails()
    {
        _factory.TrelloMock.Setup(c => c.GetBoardAsync("b1"))
            .ReturnsAsync(new TrelloBoard { Id = "b1", Name = "Sprint Board" });

        var response = await _http.GetAsync("/api/boards/b1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var board = await response.Content.ReadFromJsonAsync<TrelloBoard>();
        Assert.Equal("b1", board!.Id);
        Assert.Equal("Sprint Board", board.Name);
    }

    [Fact]
    public async Task GetBoard_Returns502_WhenClientThrows()
    {
        _factory.TrelloMock.Setup(c => c.GetBoardAsync("bad-id"))
            .ThrowsAsync(new HttpRequestException("Not Found"));

        var response = await _http.GetAsync("/api/boards/bad-id");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    // ── GET /api/boards/{boardId}/lists ───────────────────────────────────────

    [Fact]
    public async Task GetLists_Returns200_WithListCollection()
    {
        _factory.TrelloMock.Setup(c => c.GetListsAsync("b1"))
            .ReturnsAsync([
                new TrelloList { Id = "l1", Name = "To Do" },
                new TrelloList { Id = "l2", Name = "Done" },
            ]);

        var response = await _http.GetAsync("/api/boards/b1/lists");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lists = await response.Content.ReadFromJsonAsync<List<TrelloList>>();
        Assert.Equal(2, lists!.Count);
        Assert.Equal("To Do", lists[0].Name);
    }

    // ── GET /api/boards/{boardId}/cards ───────────────────────────────────────

    [Fact]
    public async Task GetCards_Returns200_WithCardCollection()
    {
        _factory.TrelloMock.Setup(c => c.GetCardsAsync("b1"))
            .ReturnsAsync([new TrelloCard { Id = "c1", Name = "Fix bug" }]);

        var response = await _http.GetAsync("/api/boards/b1/cards");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cards = await response.Content.ReadFromJsonAsync<List<TrelloCard>>();
        Assert.Single(cards!);
        Assert.Equal("Fix bug", cards![0].Name);
    }

    // ── GET /api/cards/{cardId} ───────────────────────────────────────────────

    [Fact]
    public async Task GetCard_Returns200_WithCardDetails()
    {
        _factory.TrelloMock.Setup(c => c.GetCardAsync("c1"))
            .ReturnsAsync(new TrelloCard { Id = "c1", Name = "Fix bug", Desc = "Details" });

        var response = await _http.GetAsync("/api/cards/c1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var card = await response.Content.ReadFromJsonAsync<TrelloCard>();
        Assert.Equal("c1", card!.Id);
        Assert.Equal("Fix bug", card.Name);
    }

    [Fact]
    public async Task GetCard_Returns502_WhenClientThrows()
    {
        _factory.TrelloMock.Setup(c => c.GetCardAsync("bad-id"))
            .ThrowsAsync(new HttpRequestException("Not Found"));

        var response = await _http.GetAsync("/api/cards/bad-id");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    // ── POST /api/cards ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCard_Returns201_WithNewCard()
    {
        var newCard = new TrelloCard { Id = "c2", Name = "New Task", IdList = "l1" };
        _factory.TrelloMock
            .Setup(c => c.CreateCardAsync(It.Is<CreateCardRequest>(r =>
                r.IdList == "l1" && r.Name == "New Task")))
            .ReturnsAsync(newCard);

        var body = new CreateCardRequest { IdList = "l1", Name = "New Task" };
        var response = await _http.PostAsJsonAsync("/api/cards", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var card = await response.Content.ReadFromJsonAsync<TrelloCard>();
        Assert.Equal("c2", card!.Id);
        Assert.Equal("New Task", card.Name);
    }

    [Fact]
    public async Task CreateCard_Returns502_WhenClientThrows()
    {
        _factory.TrelloMock
            .Setup(c => c.CreateCardAsync(It.IsAny<CreateCardRequest>()))
            .ThrowsAsync(new HttpRequestException("Bad Request"));

        var body = new CreateCardRequest { IdList = "l1", Name = "Task" };
        var response = await _http.PostAsJsonAsync("/api/cards", body);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    // ── PUT /api/cards/{cardId} ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateCard_Returns200_WithUpdatedCard()
    {
        var updated = new TrelloCard { Id = "c1", Name = "Renamed Task" };
        _factory.TrelloMock
            .Setup(c => c.UpdateCardAsync("c1", It.IsAny<UpdateCardRequest>()))
            .ReturnsAsync(updated);

        var body = new UpdateCardRequest { Name = "Renamed Task" };
        var response = await _http.PutAsJsonAsync("/api/cards/c1", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var card = await response.Content.ReadFromJsonAsync<TrelloCard>();
        Assert.Equal("Renamed Task", card!.Name);
    }

    // ── DELETE /api/cards/{cardId} ────────────────────────────────────────────

    [Fact]
    public async Task DeleteCard_Returns204()
    {
        _factory.TrelloMock.Setup(c => c.DeleteCardAsync("c1")).Returns(Task.CompletedTask);

        var response = await _http.DeleteAsync("/api/cards/c1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _factory.TrelloMock.Verify(c => c.DeleteCardAsync("c1"), Times.Once);
    }

    [Fact]
    public async Task DeleteCard_Returns502_WhenClientThrows()
    {
        _factory.TrelloMock.Setup(c => c.DeleteCardAsync("bad-id"))
            .ThrowsAsync(new HttpRequestException("Not Found"));

        var response = await _http.DeleteAsync("/api/cards/bad-id");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }
}
