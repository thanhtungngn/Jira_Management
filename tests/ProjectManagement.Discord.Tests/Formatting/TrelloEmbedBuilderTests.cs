using ProjectManagement.Core.Trello.Models;
using ProjectManagement.Discord.Formatting;

namespace ProjectManagement.Discord.Tests.Formatting;

/// <summary>
/// Unit tests for <see cref="TrelloEmbedBuilder"/>.
/// Pure-function tests — no mocking required.
/// </summary>
public class TrelloEmbedBuilderTests
{
    // ── BuildBoardList ─────────────────────────────────────────────────────────

    [Fact]
    public void BuildBoardList_WithBoards_RendersFields()
    {
        var boards = new List<TrelloBoard>
        {
            new() { Id = "a1", Name = "Board A", Url = "https://trello.com/b/a1" },
            new() { Id = "b2", Name = "Board B", Url = "https://trello.com/b/b2" },
        };

        var embed = TrelloEmbedBuilder.BuildBoardList(boards);

        Assert.Contains("Board", embed.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, embed.Fields.Length);
    }

    [Fact]
    public void BuildBoardList_EmptyList_ShowsNoBoardsMessage()
    {
        var embed = TrelloEmbedBuilder.BuildBoardList([]);

        Assert.Contains("No boards", embed.Description ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildBoardList_MoreThanFifteenBoards_ShowsFirstFifteen()
    {
        var boards = Enumerable.Range(1, 20)
            .Select(i => new TrelloBoard { Id = $"id{i}", Name = $"Board {i}", Url = $"https://trello.com/b/id{i}" })
            .ToList();

        var embed = TrelloEmbedBuilder.BuildBoardList(boards);

        Assert.Equal(15, embed.Fields.Length);
    }

    [Fact]
    public void BuildBoardList_ClosedBoard_ShowsClosedLabel()
    {
        var boards = new List<TrelloBoard>
        {
            new() { Id = "c1", Name = "Old Board", Closed = true, Url = "https://trello.com/b/c1" },
        };

        var embed = TrelloEmbedBuilder.BuildBoardList(boards);

        Assert.Contains("Closed", embed.Fields[0].Value, StringComparison.OrdinalIgnoreCase);
    }

    // ── BuildBoardDetail ───────────────────────────────────────────────────────

    [Fact]
    public void BuildBoardDetail_WithDescription_IncludesDescription()
    {
        var board = new TrelloBoard
        {
            Id   = "x1",
            Name = "Sprint Board",
            Url  = "https://trello.com/b/x1",
            Desc = "Our main sprint board",
        };

        var embed = TrelloEmbedBuilder.BuildBoardDetail(board);

        Assert.Equal("Sprint Board", embed.Title);
        Assert.Contains("Our main sprint board", embed.Description ?? "");
    }

    [Fact]
    public void BuildBoardDetail_OpenBoard_ShowsOpenStatus()
    {
        var board = new TrelloBoard { Id = "x2", Name = "Open Board", Url = "https://trello.com/b/x2", Closed = false };

        var embed = TrelloEmbedBuilder.BuildBoardDetail(board);

        Assert.True(embed.Fields.Any(f => f.Value.Contains("Open")));
    }

    // ── BuildCardList ──────────────────────────────────────────────────────────

    [Fact]
    public void BuildCardList_WithCards_RendersFields()
    {
        var cards = new List<TrelloCard>
        {
            new() { Id = "c1", Name = "Card 1", Url = "https://trello.com/c/c1" },
            new() { Id = "c2", Name = "Card 2", Url = "https://trello.com/c/c2" },
        };

        var embed = TrelloEmbedBuilder.BuildCardList(cards, "board1");

        Assert.Contains("Card", embed.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, embed.Fields.Length);
    }

    [Fact]
    public void BuildCardList_EmptyList_ShowsNoCardsMessage()
    {
        var embed = TrelloEmbedBuilder.BuildCardList([], "board1");

        Assert.Contains("No cards", embed.Description ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildCardList_CardWithDueDate_ShowsDueDate()
    {
        var due = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var cards = new List<TrelloCard>
        {
            new() { Id = "c1", Name = "Deadline Card", Url = "https://trello.com/c/c1", Due = due },
        };

        var embed = TrelloEmbedBuilder.BuildCardList(cards, "board1");

        Assert.Contains("2025-06-15", embed.Fields[0].Value);
    }

    [Fact]
    public void BuildCardList_CardWithLabels_ShowsLabelNames()
    {
        var cards = new List<TrelloCard>
        {
            new()
            {
                Id   = "c1",
                Name = "Labelled Card",
                Url  = "https://trello.com/c/c1",
                Labels = [new TrelloLabel { Id = "l1", Name = "Bug", Color = "red" }],
            },
        };

        var embed = TrelloEmbedBuilder.BuildCardList(cards, "board1");

        Assert.Contains("Bug", embed.Fields[0].Value);
    }

    [Fact]
    public void BuildCardList_MoreThanTenCards_ShowsFirstTen()
    {
        var cards = Enumerable.Range(1, 14)
            .Select(i => new TrelloCard { Id = $"c{i}", Name = $"Card {i}", Url = $"https://trello.com/c/c{i}" })
            .ToList();

        var embed = TrelloEmbedBuilder.BuildCardList(cards, "board1");

        Assert.Equal(10, embed.Fields.Length);
    }

    // ── BuildCardDetail ────────────────────────────────────────────────────────

    [Fact]
    public void BuildCardDetail_WithAllFields_RendersCorrectly()
    {
        var due = new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var card = new TrelloCard
        {
            Id     = "cx1",
            Name   = "Detailed Card",
            Url    = "https://trello.com/c/cx1",
            Desc   = "Some description",
            Due    = due,
            Labels = [new TrelloLabel { Id = "l1", Name = "Feature", Color = "blue" }],
        };

        var embed = TrelloEmbedBuilder.BuildCardDetail(card);

        Assert.Contains("Detailed Card", embed.Title);
        Assert.Contains("Some description", embed.Description ?? "");
        Assert.True(embed.Fields.Any(f => f.Name == "Due"));
        Assert.True(embed.Fields.Any(f => f.Name == "Labels"));
    }

    [Fact]
    public void BuildCardDetail_NoDueNoLabels_DoesNotRenderOptionalFields()
    {
        var card = new TrelloCard
        {
            Id   = "cx2",
            Name = "Simple Card",
            Url  = "https://trello.com/c/cx2",
        };

        var embed = TrelloEmbedBuilder.BuildCardDetail(card);

        Assert.DoesNotContain(embed.Fields, f => f.Name == "Due");
        Assert.DoesNotContain(embed.Fields, f => f.Name == "Labels");
    }

    // ── BuildCardCreated ───────────────────────────────────────────────────────

    [Fact]
    public void BuildCardCreated_ContainsNameAndId()
    {
        var card = new TrelloCard { Id = "new1", Name = "New Feature", Url = "https://trello.com/c/new1" };

        var embed = TrelloEmbedBuilder.BuildCardCreated(card);

        Assert.Contains("Created", embed.Title, StringComparison.OrdinalIgnoreCase);
        Assert.True(embed.Fields.Any(f => f.Value.Contains("new1")));
    }

    // ── BuildError ─────────────────────────────────────────────────────────────

    [Fact]
    public void BuildError_ContainsTitleAndMessage()
    {
        var embed = TrelloEmbedBuilder.BuildError("Trello Error", "rate limited");

        Assert.Contains("Trello Error", embed.Title);
        Assert.Contains("rate limited", embed.Description ?? "");
    }

    // ── Truncate ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null,     5, "")]
    [InlineData("",       5, "")]
    [InlineData("hi",     5, "hi")]
    [InlineData("toolong", 4, "tool…")]
    public void Truncate_HandlesVariousInputs(string? input, int max, string expected)
    {
        Assert.Equal(expected, TrelloEmbedBuilder.Truncate(input, max));
    }
}
