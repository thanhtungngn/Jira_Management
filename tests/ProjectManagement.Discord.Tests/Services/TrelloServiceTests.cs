using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProjectManagement.Core.Trello;
using ProjectManagement.Core.Trello.Models;
using ProjectManagement.Discord.Services;

namespace ProjectManagement.Discord.Tests.Services;

/// <summary>
/// Unit tests for <see cref="TrelloService"/>.
/// Uses Moq to mock <see cref="ITrelloClient"/> so no real network calls are made.
/// </summary>
public class TrelloServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (TrelloService service, Mock<ITrelloClient> clientMock) CreateService()
    {
        var mock    = new Mock<ITrelloClient>();
        var service = new TrelloService(mock.Object, NullLogger<TrelloService>.Instance);
        return (service, mock);
    }

    // ── GetBoardsAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBoardsAsync_WithBoards_ReturnsListEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.GetBoardsAsync())
            .ReturnsAsync([
                new TrelloBoard { Id = "abc", Name = "Board A", Url = "https://trello.com/b/abc" },
                new TrelloBoard { Id = "def", Name = "Board B", Url = "https://trello.com/b/def" },
            ]);

        var embed = await svc.GetBoardsAsync();

        Assert.NotNull(embed);
        Assert.Contains("Board", embed.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, embed.Fields.Length);
    }

    [Fact]
    public async Task GetBoardsAsync_EmptyList_ReturnsNoBoardsDescription()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.GetBoardsAsync()).ReturnsAsync([]);

        var embed = await svc.GetBoardsAsync();

        Assert.NotNull(embed);
        Assert.Contains("No boards", embed.Description ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetBoardsAsync_OnException_ReturnsErrorEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.GetBoardsAsync()).ThrowsAsync(new Exception("unauthorised"));

        var embed = await svc.GetBoardsAsync();

        Assert.NotNull(embed);
        Assert.Contains("Error", embed.Title, StringComparison.OrdinalIgnoreCase);
    }

    // ── GetBoardAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBoardAsync_ValidId_ReturnsDetailEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.GetBoardAsync("abc"))
            .ReturnsAsync(new TrelloBoard { Id = "abc", Name = "My Board", Url = "https://trello.com/b/abc" });

        var embed = await svc.GetBoardAsync("abc");

        Assert.NotNull(embed);
        Assert.Contains("My Board", embed.Title);
    }

    [Fact]
    public async Task GetBoardAsync_OnException_ReturnsErrorEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.GetBoardAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("not found"));

        var embed = await svc.GetBoardAsync("missing");

        Assert.NotNull(embed);
        Assert.Contains("Error", embed.Title, StringComparison.OrdinalIgnoreCase);
    }

    // ── GetCardsAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCardsAsync_WithCards_ReturnsListEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.GetCardsAsync("abc"))
            .ReturnsAsync([
                new TrelloCard { Id = "c1", Name = "Card 1", Url = "https://trello.com/c/c1" },
                new TrelloCard { Id = "c2", Name = "Card 2", Url = "https://trello.com/c/c2" },
            ]);

        var embed = await svc.GetCardsAsync("abc");

        Assert.NotNull(embed);
        Assert.Contains("Card", embed.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, embed.Fields.Length);
    }

    [Fact]
    public async Task GetCardsAsync_EmptyList_ReturnsNoCardsDescription()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.GetCardsAsync("abc")).ReturnsAsync([]);

        var embed = await svc.GetCardsAsync("abc");

        Assert.NotNull(embed);
        Assert.Contains("No cards", embed.Description ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCardsAsync_OnException_ReturnsErrorEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.GetCardsAsync(It.IsAny<string>())).ThrowsAsync(new Exception("error"));

        var embed = await svc.GetCardsAsync("abc");

        Assert.NotNull(embed);
        Assert.Contains("Error", embed.Title, StringComparison.OrdinalIgnoreCase);
    }

    // ── GetCardAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCardAsync_ValidId_ReturnsDetailEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.GetCardAsync("c1"))
            .ReturnsAsync(new TrelloCard { Id = "c1", Name = "My Card", Url = "https://trello.com/c/c1" });

        var embed = await svc.GetCardAsync("c1");

        Assert.NotNull(embed);
        Assert.Contains("My Card", embed.Title);
    }

    [Fact]
    public async Task GetCardAsync_OnException_ReturnsErrorEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.GetCardAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("not found"));

        var embed = await svc.GetCardAsync("c1");

        Assert.NotNull(embed);
        Assert.Contains("Error", embed.Title, StringComparison.OrdinalIgnoreCase);
    }

    // ── CreateCardAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCardAsync_Success_ReturnsCreatedEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.CreateCardAsync(It.IsAny<CreateCardRequest>()))
            .ReturnsAsync(new TrelloCard { Id = "new1", Name = "New Card", Url = "https://trello.com/c/new1" });

        var embed = await svc.CreateCardAsync("list1", "New Card", null);

        Assert.NotNull(embed);
        Assert.Contains("Created", embed.Title, StringComparison.OrdinalIgnoreCase);
        Assert.True(embed.Fields.Any(f => f.Value.Contains("new1")));
    }

    [Fact]
    public async Task CreateCardAsync_OnException_ReturnsErrorEmbed()
    {
        var (svc, mock) = CreateService();
        mock.Setup(c => c.CreateCardAsync(It.IsAny<CreateCardRequest>()))
            .ThrowsAsync(new Exception("bad request"));

        var embed = await svc.CreateCardAsync("list1", "New Card", null);

        Assert.NotNull(embed);
        Assert.Contains("Error", embed.Title, StringComparison.OrdinalIgnoreCase);
    }
}
