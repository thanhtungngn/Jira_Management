using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using ProjectManagement.Discord.Bot;
using ProjectManagement.Discord.Options;
using ProjectManagement.Discord.Services;
using System.ClientModel;

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

        // ── AI options ───────────────────────────────────────────────────────
        services.Configure<AiOptions>(opts =>
        {
            var section = config.GetSection(AiOptions.SectionName);
            if (section.Exists())
            {
                section.Bind(opts);
            }
            else
            {
                opts.ApiKey     = config["AI_API_KEY"]      ?? string.Empty;
                opts.Model      = config["AI_MODEL"]        ?? opts.Model;
                opts.ApiBaseUrl = config["AI_API_BASE_URL"] ?? string.Empty;
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
            LogLevel          = LogSeverity.Info,
            DefaultRunMode    = RunMode.Async,
            UseCompiledLambda = true,
        };

        services.AddSingleton(socketConfig);
        services.AddSingleton<DiscordSocketClient>();
        services.AddSingleton(sp =>
            new InteractionService(sp.GetRequiredService<DiscordSocketClient>(), interactionConfig));

        // ── Interaction handler ──────────────────────────────────────────────
        services.AddSingleton<InteractionHandler>();

        // ── OpenAI chat client (with automatic function invocation) ──────────
        services.AddSingleton<IChatClient>(sp =>
        {
            var aiOpts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value;
            return new ChatClient(aiOpts.Model, new ApiKeyCredential(aiOpts.ApiKey))
                .AsIChatClient()
                .AsBuilder()
                .UseFunctionInvocation()
                .Build(sp);
        });

        // ── Named HttpClient pointing at the deployed REST API ───────────────
        services.AddHttpClient(nameof(LlmChatService), (sp, client) =>
        {
            var aiOpts  = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value;
            var baseUrl = aiOpts.ApiBaseUrl.TrimEnd('/') + '/';
            client.BaseAddress = new Uri(baseUrl);
        });

        // ── LLM chat service ─────────────────────────────────────────────────
        services.AddScoped<ILlmChatService, LlmChatService>();

        // ── Background service that manages the bot lifecycle ────────────────
        services.AddHostedService<DiscordBotService>();
    })
    .Build();

await host.RunAsync();
