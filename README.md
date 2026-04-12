# 🧠 AI Project Management Platform Architecture

---

## 📌 Overview

This system integrates project management tools (Jira, GitHub, Trello) and exposes them via:

* REST API
* MCP (AI Tool Gateway)
* Discord Bot (AI Interface - in progress)

It is evolving into an **AI-powered engineering platform** with documentation automation and knowledge system capabilities.

### REST Route Groups

* Jira: `/api/jira/projects`, `/api/jira/issues`
* Trello: `/api/trello/boards`, `/api/trello/cards/{cardId}`
* GitHub: `/api/github/repositories`, `/api/github/repositories/{owner}/{repo}`
* Confluence: `/api/confluence/pages/{pageId}`

Legacy routes (`/api/projects`, `/api/issues`, `/api/boards`, `/api/cards`, `/api/repositories`) are still supported for compatibility.

### Latest Implemented Updates

* ✅ MCP tool `update_confluence_document` is available to update existing Confluence pages by page ID.
* ✅ Core Confluence integration is available via `ConfluenceClient`, `IConfluenceClient`, and `AddConfluenceClient`.
* ✅ API version endpoints are available for quick smoke testing: `/version` and `/api/version`.
* ✅ REST API now has grouped routes by platform: `/api/jira/*`, `/api/trello/*`, `/api/github/*`, `/api/confluence/*`.

---

# 🌐 Final Architecture (Annotated)

## Legend

* ✅ Done
* 🚧 In Progress
* ❌ Planned

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                                  CONSUMERS                                   │
│------------------------------------------------------------------------------│
│  Web / API Clients   │   AI Assistants (MCP)   │   Discord Bot 🤖 (🚧)        │
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
│ ✅ Version Endpoints     │   │ ✅ ConfluenceTools           │   │                        │
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
│ ✅ ConfluenceClient / IConfluenceClient                                      │
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
```

---

# 🏢 C4 Model

---

## 🧠 Level 1 – System Context

```
[Users: Dev / QA / PM]
        ↓
[AI Project Management Platform]
        ↓
--------------------------------------------------
| Discord Bot (🚧) | ChatGPT (MCP) | API Clients |
--------------------------------------------------
        ↓
External Systems:
- Jira ✅
- GitHub ✅
- Trello ✅
- Confluence 🚧
- Vector DB ❌
```

---

## 🧱 Level 2 – Container Diagram

```
Containers:

1. ProjectManagement.Api (REST API) ✅
   - Controllers
   - Routing
   - (Future: Auth, Rate limit)

2. ProjectManagement.Mcp (AI Gateway) ✅
   - JiraTools
   - GitHubTools
   - TrelloTools
   - DocumentationTools 🚧

3. Application Layer 🚧
   - DocOrchestrator
   - ReleaseNoteGenerator

4. AI Layer 🚧
   - LLM Client
   - PromptBuilder
   - (Future: RAG)

5. Core Layer ✅
   - JiraClient
   - GitHubClient
   - TrelloClient

6. Infrastructure ❌/🚧
   - Cache
   - Queue
   - Background jobs

7. Knowledge Layer 🚧
   - Confluence Writer
   - (Future: Vector DB)

8. Discord Bot 🚧
   - Command handler
   - MCP client
   - LLM interface
```

---

## 🔬 Level 3 – Component Diagram

### MCP Server

```
JiraTools        ✅
GitHubTools      ✅
TrelloTools      ✅
ConfluenceTools  ✅
        ↓
DocOrchestrator 🚧
```

---

### Documentation Agent

```
DocOrchestrator
 ├── SwaggerParser 🚧
 ├── PromptBuilder 🚧
 ├── LLM Client 🚧
 └── ConfluenceWriter 🚧
```

---

### Discord Bot

```
Command Handler
LLM Interpreter
MCP Client
```

---

# 🚀 Roadmap

## Phase 1

* Swagger → Confluence (manual)

## Phase 2

* MCP tool: generate docs (Confluence update tool implemented)

## Phase 3

* Discord bot integration

## Phase 4

* RAG + Knowledge system

---

# 🤖 Agent Layer (NEW)

## Overview

The system introduces a **multi-agent architecture** to support AI-assisted software development lifecycle (SDLC).

### Agents

* **BA Agent 🚧**

  * Analyze Jira/Trello tickets
  * Identify missing requirements
  * Generate clarification questions
  * Produce refined specifications & acceptance criteria

* **Dev Agent 🚧**

  * Consume refined requirements
  * Break down tasks
  * Suggest architecture/design
  * Generate implementation code

---

## Agent Flow

```
Jira / Trello Ticket
        ↓
   BA Agent 🚧
        ↓
Clarifications + Refined Spec
        ↓
   Dev Agent 🚧
        ↓
Task Breakdown + Code
```

---

## Current State

* Agents are implemented as prompt-based workflows (Markdown-driven)
* Execution via GitHub Copilot inside Visual Studio
* No direct orchestration or shared memory

---

## Target Architecture (Planned)

```
                ┌──────────────────────────────┐
                │        AGENT LAYER 🤖        │
                │------------------------------│
                │ BA Agent 🚧                  │
                │ Dev Agent 🚧                 │
                │                              │
                │ AgentOrchestrator ❌         │
                │ (workflow coordination)      │
                └──────────────┬───────────────┘
                               ▼
                        MCP / AI Layer
```

---


Future enhancements:

* Multi-agent conversation loop (BA ↔ Dev)
* Shared memory (context persistence)
* Integration with MCP tools

---

# 🎯 Key Design Highlights

* MCP-first AI architecture
* Clean separation of concerns
* Extensible integration layer
* AI orchestration ready

---

# 💼 Interview Summary

This system evolves from:

> Integration Platform → AI-powered Engineering Platform

Key capabilities:

* Multi-tool integration (Jira, GitHub, Trello)
* AI tool orchestration via MCP
* Documentation automation (in progress)
* AI assistant via Discord (in progress)

---

**End of Document**
