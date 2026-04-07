# ProjectManagement Discord Bot

A Discord bot that brings **Jira**, **Trello**, and **GitHub** project management into your server through a single natural-language `/ask` command, built on .NET 10 and [Discord.Net](https://github.com/discord-net/Discord.Net).

---

## Table of Contents

- [Features](#features)
- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
- [Running locally](#running-locally)
- [Available Commands](#available-commands)
- [Architecture](#architecture)
- [Testing](#testing)
- [Deployment](#deployment)

---

## Features

| Capability | Description |
|---|---|
| **Natural language commands** | Type anything in plain English — the LLM understands your intent |
| **Jira** | Search issues, get issue details, create issues, add comments, transition status, list projects |
| **GitHub** | List repositories, get repository, list issues, get issue |
| **Trello** | List boards, get board, list cards, get card, create card |

Users interact through a single `/ask <prompt>` slash command. The bot forwards the prompt to an OpenAI LLM which selects and calls the appropriate tool functions against your deployed REST API.

---

## Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 10.0+ |
| A Discord Application & Bot token | [Developer Portal](https://discord.com/developers/applications) |
| OpenAI API key | [platform.openai.com/api-keys](https://platform.openai.com/api-keys) |
| Deployed Project Management REST API | See [thanhtungngn/Jira_Management](https://github.com/thanhtungngn/Jira_Management) |

---

## Configuration

Copy `appsettings.example.json` to `appsettings.json` and fill in your values, **or** set environment variables:

```json
{
  "Discord": {
    "BotToken": "YOUR_DISCORD_BOT_TOKEN_HERE",
    "GuildId": null
  },
  "Ai": {
    "ApiKey": "YOUR_OPENAI_API_KEY_HERE",
    "Model": "gpt-4o-mini",
    "ApiBaseUrl": "https://your-deployed-api.onrender.com"
  }
}
```

### Getting credentials

#### Discord Bot Token
1. Go to the [Discord Developer Portal](https://discord.com/developers/applications)
2. Create a new application and navigate to the **Bot** tab
3. Click **Reset Token** to get your bot token
4. Under **OAuth2 → URL Generator**, select `bot` + `applications.commands` scopes and the **Send Messages** permission
5. Invite the bot to your server using the generated URL

#### `GuildId` (optional but recommended for development)
Setting `GuildId` to your server's ID makes slash commands register instantly.  
Leave it `null` for global commands (up to 1-hour propagation delay).

To find your server ID: enable Developer Mode in Discord settings, then right-click your server → **Copy Server ID**.

#### OpenAI API Key
1. Go to [platform.openai.com/api-keys](https://platform.openai.com/api-keys)
2. Click **Create new secret key**

#### Deployed API Base URL
Set `Ai:ApiBaseUrl` to the base URL of your already-deployed Project Management REST API  
(e.g. `https://yourapp.onrender.com`). The bot calls Jira, GitHub, and Trello operations through the LLM's tool functions against that API — no direct credentials for those services are needed here.

---

## Running locally

```bash
# 1. Clone the repository
git clone https://github.com/thanhtungngn/Jira_Management_Discord.git
cd Jira_Management_Discord

# 2. Copy and edit configuration
cp appsettings.example.json src/ProjectManagement.Discord/appsettings.json
# Fill in your credentials

# 3. Run the bot
dotnet run --project src/ProjectManagement.Discord/ProjectManagement.Discord.csproj
```

You should see:
```
info: ProjectManagement.Discord.Bot.DiscordBotService[0]
      Discord bot connected successfully
info: ProjectManagement.Discord.Bot.InteractionHandler[0]
      Slash commands registered to guild 123456789
```

---

## Available Commands

The bot exposes a single slash command that accepts natural language prompts.

### `/ask` — AI assistant

| Parameter | Required | Description |
|---|---|---|
| `prompt` | ✅ Yes | Your question or command in plain English |

#### Example prompts

```
/ask Show all open bugs in the PROJ project
/ask Create a high-priority task called "Fix login page" in project PROJ
/ask Get details for issue PROJ-42
/ask Transition PROJ-7 to Done
/ask List all my GitHub repositories
/ask Show open issues for owner/my-repo
/ask What boards do I have in Trello?
/ask List the cards on board abc123
/ask Create a card called "Design mockup" in Trello list xyz456
```

The LLM interprets your intent, calls the appropriate tool(s) against the deployed REST API, and returns a human-readable response.

---

## Architecture

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the full architecture document.

---

## Testing

```bash
# Run all tests
dotnet test DiscordBot.slnx

# Run with coverage report
dotnet test tests/ProjectManagement.Discord.Tests/ \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage
```

---

## Deployment

See [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md) for Docker, cloud (Render, Railway, Azure), and service deployment instructions.
