# Project Management

A .NET 10 solution that integrates **Jira**, **Trello**, and **GitHub** through two entry points:

| Entry point | Description |
|---|---|
| `ProjectManagement.Api` | ASP.NET Core 10 REST API with Swagger UI |
| `ProjectManagement.Mcp` | [Model Context Protocol](https://modelcontextprotocol.io/) server for AI assistant tooling |

Both share the same `ProjectManagement.Core` library for HTTP clients and configuration.

---

## Table of Contents

- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
- [Running the REST API](#running-the-rest-api)
- [Running the MCP Server](#running-the-mcp-server)
- [API Reference](#api-reference)
- [MCP Tools Reference](#mcp-tools-reference)
- [Development](#development)

---

## Architecture

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the full architecture document.

Progress : 
┌──────────────────────────────────────────────────────────────────────────────┐
│                                  CONSUMERS                                   │
│------------------------------------------------------------------------------│
│  Web / API Clients   │   AI Assistants (MCP)   │   Discord Bot 🤖 (🚧)        │
│  (Postman, FE)       │   (ChatGPT, Copilot)    │   AI Command Interface       │
└──────────────┬──────────────────────┬──────────────────────┬──────────────────┘
               │                      │                      │
               ▼                      ▼                      ▼
┌────────────────────────┐   ┌──────────────────────────────┐   ┌────────────────────────┐
│ ProjectManagement.Api  │   │ ProjectManagement.Mcp        │   │ Discord Bot Service     │
│ (ASP.NET Core)         │   │ (stdio MCP Server)           │   │ (.NET Worker) 🚧        │
│------------------------│   │------------------------------│   │------------------------│
│ ✅ Controllers          │   │ ✅ JiraTools                 │   │ 🚧 Command Handler      │
│ ✅ Routing              │   │ ✅ TrelloTools               │   │ 🚧 LLM Integration      │
│ ❌ Auth / Rate Limit    │   │ ✅ GitHubTools               │   │ 🚧 MCP Client           │
│ 🚧 Doc Endpoint         │   │ 🚧 DocumentationTools        │   │                        │
└──────────────┬─────────┘   └──────────────┬───────────────┘   └──────────────┬─────────┘
               │                            │                                  │
               └──────────────┬─────────────┴──────────────┬───────────────────┘
                              ▼                            ▼
                    ┌──────────────────────────────┐
                    │       APPLICATION LAYER       │
                    │------------------------------│
                    │ 🚧 DocOrchestrator           │
                    │ ❌ ProjectService            │
                    │ 🚧 ReleaseNoteGenerator      │
                    │ ❌ Use-case abstraction      │
                    └──────────────┬───────────────┘
                                   ▼
        ┌──────────────────────────────────────────────────────────────┐
        │                 AI / INTELLIGENCE LAYER                      │
        │--------------------------------------------------------------│
        │ 🚧 LLM Client (OpenAI / Azure)                               │
        │ 🚧 PromptBuilder                                             │
        │ ❌ Embedding Service                                         │
        │ ❌ RAG Engine                                                │
        └──────────────┬───────────────────────────────────────────────┘
                       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                     DOMAIN / CORE (EXISTING - STRONG)                        │
│------------------------------------------------------------------------------│
│ ✅ JiraClient / IJiraClient                                                  │
│ ✅ TrelloClient / ITrelloClient                                              │
│ ✅ GitHubClient / IGitHubClient                                              │
│ ✅ Models / Options                                                          │
│ ✅ Dependency Injection Extensions                                           │
└──────────────┬───────────────────────────────┬───────────────────────────────┘
               │                               │
               ▼                               ▼
┌────────────────────────────┐     ┌────────────────────────────┐
│   INFRASTRUCTURE LAYER     │     │   KNOWLEDGE / DOC LAYER     │
│----------------------------│     │-----------------------------│
│ ✅ HTTP Clients             │     │ 🚧 Confluence Writer        │
│ ❌ Redis Cache              │     │ ❌ Document Formatter       │
│ ❌ Message Queue            │     │ ❌ Template Engine          │
│ ❌ Background Jobs          │     │                             │
└──────────────┬─────────────┘     └──────────────┬──────────────┘
               │                                  │
               ▼                                  ▼
     ┌──────────────────────┐           ┌────────────────────────┐
     │ External Systems     │           │ Knowledge Storage       │
     │----------------------│           │-------------------------│
     │ ✅ Jira REST API v3   │           │ 🚧 Confluence Pages      │
     │ ✅ Trello API v1      │           │ ❌ Vector DB (RAG)       │
     │ ✅ GitHub REST API v3 │           │                         │
     └──────────────────────┘           └────────────────────────┘

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A **Jira Cloud** account with an [API token](https://id.atlassian.com/manage-profile/security/api-tokens)
- A **Trello** developer [API key and token](https://trello.com/app-key)
- A **GitHub** [personal access token](https://github.com/settings/tokens) (classic or fine-grained)

---

## Configuration

Credentials can be supplied in two ways — pick one per service:

### Option A — Structured `appsettings.json` (recommended)

Copy the example file and fill in your values:

```bash
cp appsettings.example.json src/ProjectManagement.Api/appsettings.json
cp appsettings.example.json src/ProjectManagement.Mcp/appsettings.json
```

```json
{
  "Jira": {
    "BaseUrl": "https://yourcompany.atlassian.net",
    "Email": "you@example.com",
    "ApiToken": "your-jira-api-token"
  },
  "Trello": {
    "ApiKey": "your-trello-api-key",
    "Token": "your-trello-oauth-token"
  },
  "GitHub": {
    "Token": "your-github-personal-access-token",
    "UserAgent": "ProjectManagement/1.0"
  }
}
```

### Option B — Flat environment variables (CI/CD)

```bash
JIRA_BASE_URL=https://yourcompany.atlassian.net
JIRA_EMAIL=you@example.com
JIRA_API_TOKEN=your-jira-api-token

TRELLO_API_KEY=your-trello-api-key
TRELLO_TOKEN=your-trello-oauth-token

GITHUB_TOKEN=your-github-personal-access-token
```

> The structured section is always preferred when both forms are present.

### Logging

Log levels are configured in `appsettings.json` under the `Logging` key (standard .NET logging).
Default level is `Information`. Set individual categories to `Debug` for verbose output from the HTTP clients.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "ProjectManagement": "Debug"
    }
  }
}
```

---

## Running the REST API

```bash
cd src/ProjectManagement.Api
dotnet run
```

Swagger UI is available at **`https://localhost:<port>/swagger`**.

---

## Running the MCP Server

The MCP server communicates over **stdio** and is intended to be registered with an MCP-compatible AI client (e.g. VS Code with GitHub Copilot, Claude Desktop).

```bash
cd src/ProjectManagement.Mcp
dotnet run
```

### Example `mcp.json` registration

```json
{
  "servers": {
    "project-management": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "src/ProjectManagement.Mcp"]
    }
  }
}
```

---

## API Reference

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/projects` | List all Jira projects |
| `GET` | `/api/projects/{key}` | Get a specific Jira project |
| `GET` | `/api/issues?projectKey=` | Search Jira issues with filters |
| `GET` | `/api/issues/{key}` | Get a Jira issue with comments |
| `POST` | `/api/issues` | Create a Jira issue |
| `PUT` | `/api/issues/{key}/transition` | Transition a Jira issue |
| `POST` | `/api/issues/{key}/comments` | Add a comment to a Jira issue |
| `GET` | `/api/boards` | List Trello boards |
| `GET` | `/api/boards/{id}` | Get a Trello board |
| `GET` | `/api/boards/{id}/lists` | Get lists on a Trello board |
| `GET` | `/api/cards/{boardId}` | Get cards on a Trello board |
| `POST` | `/api/cards` | Create a Trello card |
| `PUT` | `/api/cards/{id}` | Update a Trello card |
| `DELETE` | `/api/cards/{id}` | Delete a Trello card |
| `GET` | `/api/repositories` | List GitHub repositories |
| `GET` | `/api/repositories/{owner}/{repo}` | Get a GitHub repository |
| `GET` | `/api/repositories/{owner}/{repo}/branches` | List branches |
| `GET` | `/api/repositories/{owner}/{repo}/commits` | List commits |
| `GET` | `/api/repositories/{owner}/{repo}/issues` | List issues |
| `GET` | `/health` | Health check |

Full request/response schemas are in the **Swagger UI**.

---

## MCP Tools Reference

### Jira tools

| Tool | Description |
|------|-------------|
| `get_projects` | List all accessible Jira projects |
| `search_issues` | Search issues by project, status, type, or assignee |
| `get_issue` | Get full issue details including comments |
| `create_issue` | Create a new issue |
| `transition_issue` | Move an issue to a new workflow status |
| `add_comment` | Add a plain-text comment |

### Trello tools

| Tool | Description |
|------|-------------|
| `get_boards` | List all boards |
| `get_board` | Get board details |
| `get_lists` | Get lists on a board |
| `get_cards` | Get cards on a board |
| `get_card` | Get a specific card |
| `create_card` | Create a new card |
| `update_card` | Update a card |
| `delete_card` | Delete a card |

### GitHub tools

| Tool | Description |
|------|-------------|
| `list_repositories` | List accessible repositories |
| `get_repository` | Get repository details |
| `list_branches` | List branches |
| `list_commits` | List commits (with optional branch filter) |
| `list_issues` | List issues by state |
| `get_github_issue` | Get a specific issue |
| `create_github_issue` | Create a new issue |

---

## Development

### Build

```bash
dotnet build
```

### Test

```bash
dotnet test
```

### Project structure

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for design decisions, dependency graph, and data-flow diagrams.
