using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using ProjectManagement.Core.Trello;
using ProjectManagement.Core.Trello.Models;

namespace ProjectManagement.Core.Tests.Trello;

public class TrelloClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static (TrelloClient client, Mock<HttpMessageHandler> handlerMock) CreateClient(
        HttpStatusCode statusCode, object responseBody)
    {
        var json = JsonSerializer.Serialize(responseBody, JsonOptions);
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.trello.com/1/"),
        };

        return (new TrelloClient(httpClient), handlerMock);
    }

    // ── GetBoardsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBoardsAsync_ReturnsBoards()
    {
        var payload = new[]
        {
            new { id = "board1", name = "My Board", desc = "", closed = false, url = "https://trello.com/b/board1" },
        };
        var (client, _) = CreateClient(HttpStatusCode.OK, payload);

        var boards = await client.GetBoardsAsync();

        Assert.Single(boards);
        Assert.Equal("board1", boards[0].Id);
        Assert.Equal("My Board", boards[0].Name);
    }

    [Fact]
    public async Task GetBoardsAsync_ThrowsHttpRequestException_OnApiError()
    {
        var (client, _) = CreateClient(HttpStatusCode.Unauthorized, new { message = "invalid token" });

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetBoardsAsync());
    }

    // ── GetBoardAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBoardAsync_ReturnsBoard()
    {
        var payload = new { id = "board1", name = "My Board", desc = "A board", closed = false, url = "https://trello.com/b/board1" };
        var (client, _) = CreateClient(HttpStatusCode.OK, payload);

        var board = await client.GetBoardAsync("board1");

        Assert.Equal("board1", board.Id);
        Assert.Equal("My Board", board.Name);
    }

    // ── GetListsAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetListsAsync_ReturnsLists()
    {
        var payload = new[]
        {
            new { id = "list1", name = "To Do", closed = false, idBoard = "board1" },
            new { id = "list2", name = "Done",  closed = false, idBoard = "board1" },
        };
        var (client, _) = CreateClient(HttpStatusCode.OK, payload);

        var lists = await client.GetListsAsync("board1");

        Assert.Equal(2, lists.Count);
        Assert.Equal("To Do", lists[0].Name);
    }

    // ── GetCardsAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCardsAsync_ReturnsCards()
    {
        var payload = new[]
        {
            new { id = "card1", name = "Fix bug", desc = "", closed = false, idBoard = "board1", idList = "list1", url = "https://trello.com/c/card1", due = (DateTime?)null, labels = Array.Empty<object>() },
        };
        var (client, _) = CreateClient(HttpStatusCode.OK, payload);

        var cards = await client.GetCardsAsync("board1");

        Assert.Single(cards);
        Assert.Equal("card1", cards[0].Id);
        Assert.Equal("Fix bug", cards[0].Name);
    }

    // ── GetCardAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCardAsync_ReturnsCard()
    {
        var payload = new { id = "card1", name = "Fix bug", desc = "Details", closed = false, idBoard = "board1", idList = "list1", url = "https://trello.com/c/card1", due = (DateTime?)null, labels = Array.Empty<object>() };
        var (client, _) = CreateClient(HttpStatusCode.OK, payload);

        var card = await client.GetCardAsync("card1");

        Assert.Equal("card1", card.Id);
        Assert.Equal("Fix bug", card.Name);
    }

    // ── CreateCardAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCardAsync_ReturnsCreatedCard()
    {
        var payload = new { id = "card2", name = "New Task", desc = "", closed = false, idBoard = "board1", idList = "list1", url = "https://trello.com/c/card2", due = (DateTime?)null, labels = Array.Empty<object>() };
        var (client, handlerMock) = CreateClient(HttpStatusCode.OK, payload);

        var card = await client.CreateCardAsync(new CreateCardRequest { IdList = "list1", Name = "New Task" });

        Assert.Equal("card2", card.Id);
        Assert.Equal("New Task", card.Name);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
            ItExpr.IsAny<CancellationToken>());
    }

    // ── DeleteCardAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteCardAsync_SendsDeleteRequest()
    {
        var (client, handlerMock) = CreateClient(HttpStatusCode.OK, new { });

        await client.DeleteCardAsync("card1");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Delete &&
                req.RequestUri!.ToString().Contains("card1")),
            ItExpr.IsAny<CancellationToken>());
    }
}
