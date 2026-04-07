# Discord Bot — Architecture

## Overview

The Discord bot exposes Jira, Trello, and GitHub project management capabilities through a single natural-language `/ask` command. Users type plain English prompts; an OpenAI LLM interprets the intent and calls the appropriate tool functions against the already-deployed `ProjectManagement.Api` REST API.

The Discord bot has **no direct dependency** on `ProjectManagement.Core` — all platform operations go through the deployed REST API via HTTP, keeping the bot lightweight and stateless with respect to credentials.

```
┌──────────────────────────────────────────────────────────────────┐
│                          Consumers                               │
│   HTTP clients/Swagger    AI Assistants (MCP)    Discord Users   │
└────────────┬─────────────────────────┬───────────────┬───────────┘
             │                         │               │
┌────────────▼──────────┐ ┌────────────▼───────┐ ┌────▼────────────────────┐
│  ProjectManagement    │ │ ProjectManagement   │ │  ProjectManagement      │
│       .Api            │ │      .Mcp           │ │     .Discord            │
│  (ASP.NET Core 10)    │ │  (MCP stdio/HTTP)   │ │  (.NET 10 Worker)       │
│  (deployed remotely)  │ │                     │ │                         │
│                       │ │  McpServerTool      │ │  Bot/                   │
│  REST Controllers     │ │  JiraTools          │ │    DiscordBotService    │
│  Swagger UI           │ │  TrelloTools        │ │    InteractionHandler   │
└────────────▲──────────┘ │  GitHubTools        │ │  Modules/               │
             │            └────────────┬─────────┘ │    AskModule           │
             │                         │           │  Services/              │
             │  HTTP tool calls        │           │    LlmChatService      │
             └─────────────────────────┘           │  Options/               │
                  ▲ (via LlmChatService)            │    DiscordOptions      │
                  │                                │    AiOptions           │
                  └────────────────────────────────┘
                           │
                    ┌──────▼──────┐
                    │  OpenAI API │
                    │  (LLM + fn  │
                    │   calling)  │
                    └─────────────┘
```

---

## Projects

### `ProjectManagement.Discord`

**Target:** `net10.0` (Worker Service / `OutputType: Exe`)

The Discord bot entry point. Key namespaces:

| Namespace | Responsibility |
|---|---|
| `Bot/` | Discord.Net lifecycle — connect, register commands, dispatch interactions |
| `Modules/` | Thin Discord slash-command wrapper; `AskModule` defers, calls `ILlmChatService`, replies |
| `Services/` | `LlmChatService` — sends prompt to OpenAI with tool definitions; tools make HTTP calls to the deployed REST API |
| `Options/` | `DiscordOptions` and `AiOptions` POCOs bound from configuration |

### `ProjectManagement.Discord.Tests`

**Target:** `net10.0` (xUnit test project)

Unit tests covering the Discord bot's configuration classes.

---

## Layered Design

```
Discord Gateway Events
       │
       ▼
InteractionHandler         ← receives raw SocketInteraction
       │ creates SocketInteractionContext
       ▼
AskModule                  ← thin: DeferAsync → call LlmChatService → FollowupAsync
       │ calls
       ▼
LlmChatService             ← sends prompt + tool definitions to OpenAI
       │ LLM selects tools
       ▼                     (UseFunctionInvocation middleware handles agentic loop)
Tool functions             ← HTTP GET/POST to the deployed REST API
       │
       ▼
ProjectManagement.Api      ← deployed REST API (Jira, GitHub, Trello)
       │
       ▼
External APIs (Jira, Trello, GitHub)
```

### Key design decisions

1. **Natural language first** — a single `/ask <prompt>` command replaces all platform-specific slash commands. The LLM understands intent and routes to the right tool, eliminating the need for users to remember command syntax.

2. **No Core dependency in Discord** — the bot forwards requests to the deployed REST API via `HttpClient`, keeping Jira/Trello/GitHub credentials server-side. The Discord bot only needs an OpenAI API key and the deployed API URL.

3. **Automatic tool invocation loop** — `UseFunctionInvocation()` middleware (from `Microsoft.Extensions.AI`) transparently handles the LLM ↔ tool call cycle until a final text response is produced.

4. **`[ExcludeFromCodeCoverage]` on framework wrappers** — `Bot/` and `Modules/` require a live Discord WebSocket connection and are excluded from coverage measurement.

5. **Minimal gateway intents** — `GatewayIntents.Guilds` is the only intent required for slash commands.

6. **Configuration via `Microsoft.Extensions.Options`** — `AiOptions` exposes `ApiKey`, `Model`, and `ApiBaseUrl` and can be bound from the `Ai` configuration section or flat environment variables (`AI_API_KEY`, `AI_MODEL`, `AI_API_BASE_URL`).

---

## Data flow — Example: "Show all open bugs in PROJ"

```
User types /ask Show all open bugs in PROJ
       │
Discord sends InteractionCreated event to bot
       │
InteractionHandler.HandleInteractionAsync()
    → creates SocketInteractionContext
    → InteractionService.ExecuteCommandAsync()
       │
AskModule.AskAsync("Show all open bugs in PROJ")
    → await DeferAsync()           // sends "Bot is thinking…" to Discord
    → response = await _llmService.AskAsync(prompt)
    → await FollowupAsync(response)
       │
LlmChatService.AskAsync()
    → builds 15 tool definitions (Jira, GitHub, Trello)
    → sends [System, User] messages to OpenAI
       │
OpenAI decides to call tool: search_jira_issues("PROJ", null, "Bug")
       │
LlmChatService.SearchJiraIssuesAsync("PROJ", null, "Bug")
    → HTTP GET /api/issues?projectKey=PROJ&issueType=Bug&maxResults=25
    → returns JSON from deployed API
       │
OpenAI receives tool result, generates human-readable reply
       │
LlmChatService returns final text to AskModule
    → FollowupAsync sends text to Discord channel
```

---

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| `Discord.Net` | 3.19.1 | Discord WebSocket gateway, Interaction Service |
| `Microsoft.Extensions.Hosting` | 10.0.5 | Generic host, DI, IHostedService |
| `Microsoft.Extensions.Http` | 10.0.5 | IHttpClientFactory for REST API calls |
| `Microsoft.Extensions.Logging.Console` | 10.0.5 | Console log output |
| `Microsoft.Extensions.AI` | 10.4.1 | IChatClient abstraction, ChatMessage, tool definitions |
| `Microsoft.Extensions.AI.OpenAI` | 10.4.1 | OpenAI provider + UseFunctionInvocation middleware |

Test dependencies:

| Package | Version | Purpose |
|---|---|---|
| `xunit` | 2.9.3 | Test framework |
| `coverlet.collector` | 8.0.1 | Code coverage collection |
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.5 | NullLogger in tests |
