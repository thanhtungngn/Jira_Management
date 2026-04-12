# Architecture

## Overview

This solution provides unified programmatic access to four project management and documentation platforms — **Jira**, **Trello**, **GitHub**, and **Confluence** — through two independently deployable entry points that share a common core library.

```
┌──────────────────────────────────────────────────────────┐
│                     Consumers                            │
│   HTTP clients / browsers    AI assistants (MCP)         │
└────────────┬─────────────────────────┬───────────────────┘
             │                         │
┌────────────▼────────┐   ┌────────────▼────────────────────┐
│  ProjectManagement  │   │     ProjectManagement.Mcp        │
│       .Api          │   │   (stdio MCP server, net10.0)    │
│  (ASP.NET Core 10)  │   │                                  │
│                     │   │  JiraTools / TrelloTools /       │
│  Controllers:       │   │  GitHubTools / ConfluenceTools   │
│  - ProjectsCtrl     │   │  (McpServerToolType)             │
│  - IssuesCtrl       │   └────────────┬────────────────────┘
│  - BoardsCtrl       │                │
│  - CardsCtrl        │                │
│  - RepositoriesCtrl │                │
│  - HealthCtrl       │                │
│  - Version Endpoints│                │
└────────────┬────────┘                │
             │                         │
             └──────────┬──────────────┘
                        │
        ┌───────────────▼───────────────────────────────┐
        │           ProjectManagement.Core               │
        │               (net10.0 class library)          │
        │                                                │
        │  ServiceCollectionExtensions                   │
        │    AddJiraClient() / AddTrelloClient() /       │
        │    AddGitHubClient() / AddConfluenceClient()   │
        │                                                │
        │  ┌──────────────┐  ┌────────────┐  ┌────────┐  ┌──────────────┐ │
        │  │  JiraClient  │  │TrelloClient│  │GitHub  │  │Confluence    │ │
        │  │  IJiraClient │  │ITrelloClient│ │Client  │  │Client        │ │
        │  │  JiraOptions │  │TrelloOptions│ │IGitHub │  │IConfluence   │ │
        │  │  Models/     │  │Models/     │  │Options │  │Options/Models│ │
        │  └──────┬───────┘  └─────┬──────┘  └───┬────┘  └──────┬───────┘ │
        └─────────┼────────────────┼──────────────┼──────────────┼─────────┘
            │                │              │              │
         ┌────────▼──────┐ ┌───────▼────┐ ┌──────▼────────┐ ┌───▼──────────┐
         │  Jira Cloud   │ │   Trello   │ │  GitHub REST  │ │ Confluence   │
         │  REST API v3  │ │   API v1   │ │     API v3    │ │ REST API     │
         └───────────────┘ └────────────┘ └───────────────┘ └──────────────┘
```

---

## Projects

### `ProjectManagement.Core`

**Target:** `net10.0` (class library)

The shared kernel. All HTTP communication, authentication, model definitions, and DI registration live here.

#### Key types

| Type | Responsibility |
|------|----------------|
| `ServiceCollectionExtensions` | `AddJiraClient`, `AddTrelloClient`, `AddGitHubClient`, `AddConfluenceClient` — registers typed `HttpClient` instances and binds options |
| `JiraClient` / `IJiraClient` | Jira Cloud REST API v3 — projects, issues, transitions, comments |
| `TrelloClient` / `ITrelloClient` | Trello API v1 — boards, lists, cards (CRUD) |
| `GitHubClient` / `IGitHubClient` | GitHub REST API v3 — repositories, branches, commits, issues |
| `ConfluenceClient` / `IConfluenceClient` | Confluence REST API — page update by page ID with storage-format body and version increment |
| `JiraOptions` | `BaseUrl`, `Email`, `ApiToken` — section name `Jira` |
| `TrelloOptions` | `ApiKey`, `Token` — section name `Trello` |
| `GitHubOptions` | `Token`, `UserAgent` — section name `GitHub` |
| `ConfluenceOptions` | `BaseUrl`, `Email`, `ApiToken` — section name `Confluence` |

#### Authentication per service

| Service | Scheme | Header |
|---------|--------|--------|
| Jira | HTTP Basic (`email:token` → Base64) | `Authorization: Basic …` |
| Trello | OAuth 1.0 (key + token) | `Authorization: OAuth oauth_consumer_key="…", oauth_token="…"` |
| GitHub | Bearer token | `Authorization: Bearer …` |
| Confluence | HTTP Basic (`email:token` → Base64) | `Authorization: Basic …` |

#### Configuration resolution

`ServiceCollectionExtensions` resolves options in priority order:

1. **Structured section** (`Jira:BaseUrl`, `Trello:ApiKey`, `GitHub:Token`, `Confluence:BaseUrl`, …) — preferred
2. **Flat environment variable fallback** (`JIRA_BASE_URL`, `TRELLO_API_KEY`, `GITHUB_TOKEN`, `CONFLUENCE_BASE_URL`, …)

This allows both `appsettings.json`-based configuration (local development) and plain environment variable injection (Docker, CI/CD).

#### Logging

All three clients use `ILogger<T>` injected via DI (falls back to `NullLogger` for test/standalone use):

- `LogDebug` — before each HTTP call (URL, key parameters)
- `LogInformation` — after successful response (result count, entity IDs/names)
- `LogWarning` / `LogError` — surfaced through the API's global exception handler

---

### `ProjectManagement.Api`

**Target:** `net10.0` (ASP.NET Core Web)  
**Port:** configured in `launchSettings.json` (default HTTPS `62693`)

A standard ASP.NET Core REST API. All controllers are thin: they delegate entirely to the `Core` clients and return `ActionResult<T>`.

#### Controllers

| Controller | Route prefix | Backing client |
|------------|-------------|----------------|
| `JiraController` | `/api/jira/*` (legacy: `/api/projects`, `/api/issues`) | `IJiraClient` |
| `TrelloController` | `/api/trello/*` (legacy: `/api/boards`, `/api/cards`) | `ITrelloClient` |
| `RepositoriesController` | `/api/repositories` | `IGitHubClient` |
| `ConfluenceController` | `/api/confluence/pages` | `IConfluenceClient` |
| `HealthController` | `/api/health` | — |

#### Minimal endpoints for smoke tests

| Endpoint | Purpose |
|----------|---------|
| `/version` | Returns service name and build version |
| `/api/version` | Alias for `/version`, useful for API-prefixed probes |

#### Platform-grouped route aliases

To make the API surface easier to discover by product area, every domain now has a grouped route prefix.
Legacy routes are still supported for backward compatibility.

| Platform | Grouped prefix | Example |
|----------|----------------|---------|
| Jira | `/api/jira` | `/api/jira/projects`, `/api/jira/issues` |
| Trello | `/api/trello` | `/api/trello/boards`, `/api/trello/cards/{cardId}` |
| GitHub | `/api/github` | `/api/github/repositories`, `/api/github/repositories/{owner}/{repo}` |
| Confluence | `/api/confluence` | `/api/confluence/pages/{pageId}` |

#### Cross-cutting concerns

- **Swagger/OpenAPI** — generated from XML doc comments, versioned via `AssemblyInformationalVersion`
- **Global exception handler** — `UseExceptionHandler` middleware:
  - `HttpRequestException` → `502 Bad Gateway` with `application/problem+json` body
  - All other exceptions → `500 Internal Server Error`
- **Logging** — `ILogger<TController>` injected into every controller; logs method, route, and key parameters at `Information` level

#### Pipeline order

```
Request
  → HTTPS redirection
  → Exception handler (global)
  → Authorization
  → Controller routing
  → Controller action (→ Core client → external API)
Response
```

---

### `ProjectManagement.Mcp`

**Target:** `net10.0` (Console / `OutputType=Exe`)  
**Transport:** stdio (standard Model Context Protocol convention)

Exposes all four service integrations as **MCP tools** so AI assistants (GitHub Copilot, Claude Desktop, etc.) can invoke them directly.

#### Tool classes

| Class | Tools exposed | Backing client |
|-------|---------------|----------------|
| `JiraTools` | `get_projects`, `search_issues`, `get_issue`, `create_issue`, `transition_issue`, `add_comment` | `IJiraClient` |
| `TrelloTools` | `get_boards`, `get_board`, `get_lists`, `get_cards`, `get_card`, `create_card`, `update_card`, `delete_card` | `ITrelloClient` |
| `GitHubTools` | `list_repositories`, `get_repository`, `list_branches`, `list_commits`, `list_issues`, `get_github_issue`, `create_github_issue` | `IGitHubClient` |
| `ConfluenceTools` | `update_confluence_document` | `IConfluenceClient` |

Each tool class:
- is decorated with `[McpServerToolType]`
- receives `ILogger<T>` via constructor injection and logs every tool invocation at `Information` level with a `[MCP]` prefix
- delegates immediately to the corresponding `Core` client method

#### Startup

```csharp
Host.CreateApplicationBuilder(args)
  → AddJiraClient / AddTrelloClient / AddGitHubClient / AddConfluenceClient  (from Core)
    → AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly()
    → Build().RunAsync()
```

---

## Dependency graph

```
ProjectManagement.Api   ProjectManagement.Mcp
         │                       │
         └──────────┬────────────┘
                    ▼
         ProjectManagement.Core
                    │
         Microsoft.Extensions.*
         System.Net.Http
         System.Text.Json
```

Test projects:

```
ProjectManagement.Api.Tests   → ProjectManagement.Api + ProjectManagement.Core (via DI helpers)
ProjectManagement.Core.Tests  → ProjectManagement.Core
ProjectManagement.Mcp.Tests   → ProjectManagement.Mcp + ProjectManagement.Core
```

---

## Data flow — REST API request

```
Client
  │  GET /api/issues?projectKey=PROJ&status=Open
  ▼
JiraController.SearchIssues(request)
  │  _logger.LogInformation(...)
  ▼
IJiraClient.SearchIssuesAsync(request)
  │  _logger.LogDebug("Searching issues with JQL: {Jql}", jql)
  ▼
HttpClient → POST https://<baseUrl>/rest/api/3/search/jql
             body: { jql, maxResults, fields, nextPageToken? }
  ▼
Jira Cloud REST API
  ▼
SearchAndReconcileResults → SearchResult (deserialized via System.Text.Json)
  │  _logger.LogInformation("Retrieved {Count} issues", ...)
  ▼
200 OK  application/json
```

---

## Data flow — MCP tool invocation

```
AI assistant (stdio)
  │  {"method": "tools/call", "params": {"name": "search_issues", ...}}
  ▼
ModelContextProtocol runtime
  ▼
JiraTools.SearchIssuesAsync(projectKey, ...)
  │  _logger.LogInformation("[MCP] search_issues: ...")
  ▼
IJiraClient.SearchIssuesAsync(request)      (same as REST path from here)
  ▼
Jira Cloud REST API
  ▼
SearchResult → serialized as MCP tool result
  ▼
AI assistant (stdio)
```

---

## Configuration loading order

Both entry points use the standard .NET configuration stack:

1. `appsettings.json` (base defaults)
2. Environment variables (override — useful in Docker/CI)
3. `ServiceCollectionExtensions` option binder (structured section → flat key fallback)

The `appsettings.example.json` at the root documents all required keys and can be copied to either project directory.

---

## Logging architecture

| Layer | Logger category | Default level | What is logged |
|-------|----------------|---------------|----------------|
| API controllers | `ProjectManagement.Api.Controllers.*` | Information | Incoming operation + key params |
| MCP tools | `ProjectManagement.Mcp.*` | Information | Tool name + key params (prefixed `[MCP]`) |
| Core clients | `ProjectManagement.Core.Jira.JiraClient` etc. | Debug / Information | HTTP call details, result counts |
| ASP.NET Core | `Microsoft.AspNetCore.*` | Warning | Framework internals |
| HttpClient | `System.Net.Http.HttpClient` | Warning | Raw HTTP (disabled by default) |

To enable verbose HTTP tracing, set `"System.Net.Http.HttpClient": "Trace"` in the `Logging:LogLevel` section.
