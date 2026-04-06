using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectManagement.Discord.Options;

namespace ProjectManagement.Discord.Bot;

/// <summary>
/// <see cref="IHostedService"/> that manages the <see cref="DiscordSocketClient"/> lifecycle:
/// logs in, starts, and gracefully stops the bot.
/// </summary>
/// <remarks>
/// This class is a Discord.Net integration wrapper and is excluded from coverage measurement.
/// Unit-testable business logic belongs in the <c>Services/</c> layer.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class DiscordBotService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionHandler  _interactionHandler;
    private readonly DiscordOptions      _options;
    private readonly ILogger<DiscordBotService> _logger;

    /// <summary>
    /// Initialises the service with all required dependencies.
    /// </summary>
    public DiscordBotService(
        DiscordSocketClient        client,
        InteractionHandler         interactionHandler,
        IOptions<DiscordOptions>   options,
        ILogger<DiscordBotService> logger)
    {
        _client             = client;
        _interactionHandler = interactionHandler;
        _options            = options.Value;
        _logger             = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Validate the bot token before attempting to connect.
        if (string.IsNullOrWhiteSpace(_options.BotToken))
            throw new InvalidOperationException(
                "Discord BotToken is required (Discord:BotToken or DISCORD_BOT_TOKEN).");

        // Wire up Discord.Net log events → Microsoft.Extensions.Logging.
        _client.Log += LogDiscordMessageAsync;

        // Initialise the interaction handler (registers modules, hooks events).
        await _interactionHandler.InitialiseAsync();

        // Authenticate and connect to the Discord gateway.
        await _client.LoginAsync(TokenType.Bot, _options.BotToken);
        await _client.StartAsync();

        _logger.LogInformation("Discord bot connected successfully");
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Discord bot shutting down…");
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Forwards Discord.Net <see cref="LogMessage"/> events to the .NET logger
    /// using the appropriate severity level.
    /// </summary>
    private Task LogDiscordMessageAsync(LogMessage msg)
    {
        var level = msg.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error    => LogLevel.Error,
            LogSeverity.Warning  => LogLevel.Warning,
            LogSeverity.Info     => LogLevel.Information,
            LogSeverity.Verbose  => LogLevel.Debug,
            LogSeverity.Debug    => LogLevel.Trace,
            _                    => LogLevel.Information,
        };

        // Include exception details when present.
        if (msg.Exception is not null)
            _logger.Log(level, msg.Exception, "[Discord] {Source}: {Message}", msg.Source, msg.Message);
        else
            _logger.Log(level, "[Discord] {Source}: {Message}", msg.Source, msg.Message);

        return Task.CompletedTask;
    }
}
