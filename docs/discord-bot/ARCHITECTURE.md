# Discord Bot — Architecture

## Overview

The Discord bot is an additional entry point in the `ProjectManagement` solution that exposes the same Jira, Trello, and GitHub capabilities through Discord slash commands. It shares the `ProjectManagement.Core` library with the REST API and MCP server.

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
│                       │ │                     │ │                         │
│  REST Controllers     │ │  McpServerTool      │ │  Bot/                   │
│  Swagger UI           │ │  JiraTools          │ │    DiscordBotService    │
└────────────┬──────────┘ │  TrelloTools        │ │    InteractionHandler   │
             │            │  GitHubTools         │ │  Modules/               │
             │            └────────────┬─────────┘ │    JiraModule           │
             │                         │           │    GitHubModule         │
             │                         │           │    TrelloModule         │
             │                         │           │  Services/              │
             │                         │           │    JiraService          │
             │                         │           │    GitHubService        │
             │                         │           │    TrelloService        │
             │                         │           │  Formatting/            │
             │                         │           │    JiraEmbedBuilder     │
             │                         │           │    GitHubEmbedBuilder   │
             │                         │           │    TrelloEmbedBuilder   │
             └──────────┬──────────────┘           └────────┬────────────────┘
                        │                                   │
        ┌───────────────▼───────────────────────────────────┘
        │           ProjectManagement.Core                        
        │               (net10.0 class library)                   
        │                                                         
        │  ServiceCollectionExtensions                            
        │    AddJiraClient() / AddTrelloClient() / AddGitHubClient()
        │                                                         
        │  ┌──────────────┐  ┌────────────┐  ┌───────────────┐  │
        │  │  JiraClient  │  │TrelloClient│  │  GitHubClient │  │
        │  │  IJiraClient │  │ITrelloClient│ │ IGitHubClient │  │
        │  └──────┬───────┘  └─────┬──────┘  └──────┬────────┘  │
        └─────────┼────────────────┼─────────────────┼───────────┘
                  │                │                 │
         ┌────────▼──────┐ ┌───────▼────┐ ┌─────────▼─────────┐
         │  Jira Cloud   │ │   Trello   │ │   GitHub REST API  │
         │  REST API v3  │ │   API v1   │ │        v3          │
         └───────────────┘ └────────────┘ └───────────────────┘
```

---

## Projects

### `ProjectManagement.Discord`

**Target:** `net10.0` (Worker Service / `OutputType: Exe`)

The Discord bot entry point. Key namespaces:

| Namespace | Responsibility |
|---|---|
| `Bot/` | Discord.Net lifecycle — connect, register commands, dispatch interactions |
| `Modules/` | Thin Discord slash-command wrappers; each method calls a service then formats a reply |
| `Services/` | Business logic — calls `ProjectManagement.Core` clients, returns `Discord.Embed` objects |
| `Formatting/` | Static helpers that convert domain models (Jira/Trello/GitHub) to Discord `EmbedBuilder` instances |
| `Options/` | `DiscordOptions` POCO bound from configuration |

### `ProjectManagement.Discord.Tests`

**Target:** `net10.0` (xUnit test project)

Unit tests covering ≥ 86% of lines in `ProjectManagement.Discord`.

---

## Layered Design

```
Discord Gateway Events
       │
       ▼
InteractionHandler         ← receives raw SocketInteraction
       │ creates SocketInteractionContext
       ▼
Modules (JiraModule etc.)  ← thin: DeferAsync → call service → FollowupAsync
       │ calls
       ▼
Services (JiraService etc.)← business logic: calls Core client, formats embed
       │ calls
       ▼
Core Clients (IJiraClient etc.) ← HTTP calls to Jira / Trello / GitHub APIs
       │
       ▼
External APIs
```

### Key design decisions

1. **Separation of concerns** — Discord.Net ceremony (gateway, interactions) is isolated in `Bot/` and `Modules/`. All actual logic is in `Services/`, which depends only on `ProjectManagement.Core` interfaces and is fully unit-testable without any Discord connection.

2. **`[ExcludeFromCodeCoverage]` on framework wrappers** — `Bot/` and `Modules/` classes require a live Discord WebSocket connection, so they are excluded from coverage measurement. The testable `Services/` and `Formatting/` layers have 100% line coverage.

3. **Embed builders are static pure functions** — `JiraEmbedBuilder`, `GitHubEmbedBuilder`, and `TrelloEmbedBuilder` are all static classes with no state. This makes them easy to unit-test and reuse.

4. **Configuration via `Microsoft.Extensions.Options`** — `DiscordOptions` follows the same pattern as `JiraOptions`, `TrelloOptions`, and `GitHubOptions` in the Core library: bound from a named section or from flat environment variables.

5. **Minimal gateway intents** — `GatewayIntents.Guilds` is the only intent required for slash commands, reducing permissions and improving security posture.

---

## Data flow — Example: `/jira search PROJ`

```
User types /jira search PROJ
       │
Discord sends InteractionCreated event to bot
       │
InteractionHandler.HandleInteractionAsync()
    → creates SocketInteractionContext
    → InteractionService.ExecuteCommandAsync()
       │
JiraModule.SearchAsync("PROJ", null, null)
    → await DeferAsync()           // sends "Bot is thinking…" to Discord
    → embed = await _jiraService.SearchIssuesAsync("PROJ", null, null)
    → await FollowupAsync(embed)   // sends the result embed to Discord
       │
JiraService.SearchIssuesAsync()
    → await _client.SearchIssuesAsync(new SearchIssuesRequest { … })
    → JiraEmbedBuilder.BuildIssueList(result, "PROJ")
    → returns Discord.Embed
       │
JiraClient.SearchIssuesAsync()
    → HTTP GET /rest/api/3/search?jql=project=PROJ
    → deserialises to SearchResult
```

---

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| `Discord.Net` | 3.19.1 | Discord WebSocket gateway, Interaction Service |
| `Microsoft.Extensions.Hosting` | 10.0.5 | Generic host, DI, IHostedService |
| `Microsoft.Extensions.Http` | 10.0.5 | IHttpClientFactory (via Core) |
| `Microsoft.Extensions.Logging.Console` | 10.0.5 | Console log output |
| `ProjectManagement.Core` | *(project ref)* | Jira, Trello, GitHub HTTP clients |

Test dependencies:

| Package | Version | Purpose |
|---|---|---|
| `xunit` | 2.9.3 | Test framework |
| `Moq` | 4.20.72 | Mocking `IJiraClient`, `IGitHubClient`, `ITrelloClient` |
| `coverlet.collector` | 8.0.1 | Code coverage collection |
| `Discord.Net` | 3.19.1 | `EmbedBuilder` types used in assertions |
