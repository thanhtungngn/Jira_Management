using DiscordColor = global::Discord.Color;

namespace ProjectManagement.Discord.Formatting;

/// <summary>
/// Centralised colour palette used across all Discord embeds produced by this bot.
/// Values are standard 24-bit RGB integers.
/// </summary>
public static class EmbedColors
{
    /// <summary>Blue — used for informational / list responses.</summary>
    public static readonly DiscordColor Info = new(0x3498DB);

    /// <summary>Green — used for success / create confirmations.</summary>
    public static readonly DiscordColor Success = new(0x2ECC71);

    /// <summary>Orange — used for transition / update confirmations.</summary>
    public static readonly DiscordColor Warning = new(0xE67E22);

    /// <summary>Red — used for error responses.</summary>
    public static readonly DiscordColor Error = new(0xE74C3C);

    /// <summary>Purple — used for GitHub responses.</summary>
    public static readonly DiscordColor GitHub = new(0x6E40C9);

    /// <summary>Teal — used for Trello responses.</summary>
    public static readonly DiscordColor Trello = new(0x0079BF);

    /// <summary>Blue — used for Jira responses.</summary>
    public static readonly DiscordColor Jira = new(0x0052CC);
}
