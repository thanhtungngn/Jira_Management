namespace ProjectManagement.Discord.Options;

/// <summary>
/// Configuration settings for the Discord bot.
/// Bind from the <c>Discord</c> section in <c>appsettings.json</c> or via environment variables.
/// </summary>
public sealed class DiscordOptions
{
    /// <summary>The configuration section name used to bind this class.</summary>
    public const string SectionName = "Discord";

    /// <summary>
    /// The Discord Bot Token obtained from the Discord Developer Portal.
    /// Required. Set via <c>Discord:BotToken</c> or <c>DISCORD_BOT_TOKEN</c>.
    /// </summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>
    /// Optional guild (server) ID for registering slash commands.
    /// When set, commands are registered only in this guild (instant, good for development).
    /// When <c>null</c> or empty, commands are registered globally (up to 1-hour propagation).
    /// </summary>
    public ulong? GuildId { get; set; }
}
