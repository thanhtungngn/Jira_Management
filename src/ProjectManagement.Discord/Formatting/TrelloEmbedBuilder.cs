using Discord;
using ProjectManagement.Core.Trello.Models;

namespace ProjectManagement.Discord.Formatting;

/// <summary>
/// Converts Trello domain objects into Discord <see cref="Embed"/> instances.
/// All methods are static pure functions to keep them easy to unit-test.
/// </summary>
public static class TrelloEmbedBuilder
{
    /// <summary>
    /// Builds an embed listing Trello boards.
    /// </summary>
    /// <param name="boards">The boards to display.</param>
    public static Embed BuildBoardList(List<TrelloBoard> boards)
    {
        var builder = new EmbedBuilder()
            .WithTitle("📌 Trello Boards")
            .WithColor(EmbedColors.Trello)
            .WithFooter($"{boards.Count} board(s)");

        if (boards.Count == 0)
        {
            builder.WithDescription("No boards found.");
            return builder.Build();
        }

        foreach (var b in boards.Take(15))
        {
            var status = b.Closed ? "🔒 Closed" : "✅ Open";
            builder.AddField(b.Name, $"ID: `{b.Id}` | {status}", inline: false);
        }

        return builder.Build();
    }

    /// <summary>
    /// Builds a detailed embed for a single <see cref="TrelloBoard"/>.
    /// </summary>
    /// <param name="board">The board to display.</param>
    public static Embed BuildBoardDetail(TrelloBoard board)
    {
        var builder = new EmbedBuilder()
            .WithTitle(board.Name)
            .WithUrl(board.Url)
            .WithColor(EmbedColors.Trello)
            .AddField("Status", board.Closed ? "🔒 Closed" : "✅ Open", inline: true)
            .AddField("ID",     $"`{board.Id}`",                         inline: true);

        if (!string.IsNullOrWhiteSpace(board.Desc))
            builder.WithDescription(Truncate(board.Desc, 400));

        return builder.Build();
    }

    /// <summary>
    /// Builds an embed listing Trello cards on a board.
    /// </summary>
    /// <param name="cards">The cards to display.</param>
    /// <param name="boardId">The board ID used in the query.</param>
    public static Embed BuildCardList(List<TrelloCard> cards, string boardId)
    {
        var builder = new EmbedBuilder()
            .WithTitle($"🃏 Trello Cards — Board `{boardId}`")
            .WithColor(EmbedColors.Trello)
            .WithFooter($"{cards.Count} card(s)");

        if (cards.Count == 0)
        {
            builder.WithDescription("No cards found on this board.");
            return builder.Build();
        }

        foreach (var c in cards.Take(10))
        {
            var labels = c.Labels.Count > 0
                ? string.Join(", ", c.Labels.Select(l => l.Name))
                : "—";
            var due = c.Due.HasValue ? c.Due.Value.ToString("yyyy-MM-dd") : "—";
            builder.AddField(
                Truncate(c.Name, 90),
                $"ID: `{c.Id}` | Due: {due} | Labels: {labels}",
                inline: false);
        }

        return builder.Build();
    }

    /// <summary>
    /// Builds a detailed embed for a single <see cref="TrelloCard"/>.
    /// </summary>
    /// <param name="card">The card to display.</param>
    public static Embed BuildCardDetail(TrelloCard card)
    {
        var builder = new EmbedBuilder()
            .WithTitle(Truncate(card.Name, 200))
            .WithUrl(card.Url)
            .WithColor(EmbedColors.Trello)
            .AddField("Status", card.Closed ? "🔒 Closed" : "✅ Open", inline: true)
            .AddField("ID",     $"`{card.Id}`",                         inline: true);

        if (card.Due.HasValue)
            builder.AddField("Due", card.Due.Value.ToString("yyyy-MM-dd"), inline: true);

        if (card.Labels.Count > 0)
            builder.AddField("Labels",
                string.Join(", ", card.Labels.Select(l => $"{l.Name} ({l.Color ?? "none"})")),
                inline: false);

        if (!string.IsNullOrWhiteSpace(card.Desc))
            builder.WithDescription(Truncate(card.Desc, 400));

        return builder.Build();
    }

    /// <summary>
    /// Builds a confirmation embed for a newly created Trello card.
    /// </summary>
    /// <param name="card">The created card.</param>
    public static Embed BuildCardCreated(TrelloCard card)
    {
        return new EmbedBuilder()
            .WithTitle("✅ Trello Card Created")
            .WithColor(EmbedColors.Success)
            .WithUrl(card.Url)
            .AddField("Name", Truncate(card.Name, 200), inline: false)
            .AddField("ID",   $"`{card.Id}`",           inline: true)
            .Build();
    }

    /// <summary>
    /// Builds a generic error embed with a red colour.
    /// </summary>
    /// <param name="title">Short error title.</param>
    /// <param name="message">The human-readable error message.</param>
    public static Embed BuildError(string title, string message)
    {
        return new EmbedBuilder()
            .WithTitle($"❌ {title}")
            .WithColor(EmbedColors.Error)
            .WithDescription(Truncate(message, 900))
            .Build();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Truncates a string to <paramref name="maxLength"/> characters, appending "…" if truncated.</summary>
    public static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Length <= maxLength ? text : text[..maxLength] + "…";
    }
}
