using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectManagement.Core;
using ProjectManagement.Discord.Bot;
using ProjectManagement.Discord.Options;
using ProjectManagement.Discord.Services;

// ── Host ──────────────────────────────────────────────────────────────────────
var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(cfg =>
    {
        // appsettings.json → appsettings.{env}.json → environment variables
        cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        cfg.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json",
            optional: true, reloadOnChange: true);
        cfg.AddEnvironmentVariables();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .ConfigureServices((ctx, services) =>
    {
        var config = ctx.Configuration;

        // ── Discord options ──────────────────────────────────────────────────
        services.Configure<DiscordOptions>(opts =>
        {
            // Prefer structured section; fall back to a flat env var
            var section = config.GetSection(DiscordOptions.SectionName);
            if (section.Exists())
            {
                section.Bind(opts);
            }
            else
            {
                opts.BotToken = config["DISCORD_BOT_TOKEN"] ?? string.Empty;
                if (ulong.TryParse(config["DISCORD_GUILD_ID"], out var guildId))
                    opts.GuildId = guildId;
            }
        });

        // ── Discord.Net clients ──────────────────────────────────────────────
        var socketConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds, // minimal intents needed for slash commands
            LogLevel       = LogSeverity.Info,
        };

        var interactionConfig = new InteractionServiceConfig
        {
            LogLevel             = LogSeverity.Info,
            DefaultRunMode       = RunMode.Async,
            UseCompiledLambda    = true,
        };

        services.AddSingleton(socketConfig);
        services.AddSingleton<DiscordSocketClient>();
        services.AddSingleton(sp =>
            new InteractionService(sp.GetRequiredService<DiscordSocketClient>(), interactionConfig));

        // ── Interaction handler ──────────────────────────────────────────────
        services.AddSingleton<InteractionHandler>();

        // ── Project Management Core clients ──────────────────────────────────
        services.AddJiraClient(config);
        services.AddTrelloClient(config);
        services.AddGitHubClient(config);

        // ── Discord command services (business logic) ─────────────────────────
        services.AddScoped<IJiraService, JiraService>();
        services.AddScoped<IGitHubService, GitHubService>();
        services.AddScoped<ITrelloService, TrelloService>();

        // ── Background service that manages the bot lifecycle ────────────────
        services.AddHostedService<DiscordBotService>();
    })
    .Build();

await host.RunAsync();
