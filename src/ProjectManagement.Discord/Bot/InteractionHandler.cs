using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectManagement.Discord.Options;

namespace ProjectManagement.Discord.Bot;

/// <summary>
/// Registers interaction modules (slash commands) with Discord's Interaction Service
/// and dispatches incoming interactions to the correct handler.
/// </summary>
/// <remarks>
/// This class is a Discord.Net integration wrapper and is excluded from coverage measurement.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class InteractionHandler
{
    private readonly DiscordSocketClient  _client;
    private readonly InteractionService   _interactionService;
    private readonly IServiceProvider     _serviceProvider;
    private readonly DiscordOptions       _options;
    private readonly ILogger<InteractionHandler> _logger;

    /// <summary>
    /// Initialises the handler.
    /// </summary>
    public InteractionHandler(
        DiscordSocketClient          client,
        InteractionService           interactionService,
        IServiceProvider             serviceProvider,
        IOptions<DiscordOptions>     options,
        ILogger<InteractionHandler>  logger)
    {
        _client             = client;
        _interactionService = interactionService;
        _serviceProvider    = serviceProvider;
        _options            = options.Value;
        _logger             = logger;
    }

    /// <summary>
    /// Discovers and registers all <see cref="InteractionModuleBase{T}"/> modules in this
    /// assembly and hooks the necessary Discord gateway events.
    /// </summary>
    public async Task InitialiseAsync()
    {
        // Discover all modules in the current assembly.
        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

        // When the client is ready, register slash commands with Discord.
        _client.Ready += RegisterCommandsAsync;

        // Dispatch incoming interaction payloads.
        _client.InteractionCreated += HandleInteractionAsync;

        // Log any errors produced by the Interaction Service.
        _interactionService.Log += msg =>
        {
            _logger.LogDebug("[InteractionService] {Message}", msg.ToString());
            return Task.CompletedTask;
        };
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Called once when the gateway signals the bot is ready.
    /// Registers commands either globally or to a specific guild.
    /// </summary>
    private async Task RegisterCommandsAsync()
    {
        if (_options.GuildId.HasValue)
        {
            // Guild-scoped commands propagate immediately (ideal for development).
            await _interactionService.RegisterCommandsToGuildAsync(_options.GuildId.Value);
            _logger.LogInformation("Slash commands registered to guild {GuildId}", _options.GuildId.Value);
        }
        else
        {
            // Global commands take up to one hour to propagate.
            await _interactionService.RegisterCommandsGloballyAsync();
            _logger.LogInformation("Slash commands registered globally");
        }
    }

    /// <summary>
    /// Creates a typed <see cref="SocketInteractionContext"/> and executes the command.
    /// </summary>
    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        try
        {
            var ctx    = new SocketInteractionContext(_client, interaction);
            var result = await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider);

            if (!result.IsSuccess)
                _logger.LogWarning("Interaction error: {Error} — {Reason}", result.Error, result.ErrorReason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing interaction");

            // Attempt to acknowledge the interaction so it doesn't show a failure to the user.
            if (interaction.Type == global::Discord.InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync().ContinueWith(_ => Task.CompletedTask);
        }
    }
}
