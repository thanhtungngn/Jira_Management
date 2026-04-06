# ProjectManagement Discord Bot

A Discord bot that exposes **Jira**, **Trello**, and **GitHub** project management operations as slash commands, built on .NET 10 and [Discord.Net](https://github.com/discord-net/Discord.Net).

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

| Platform | Commands |
|---|---|
| **Jira** | Search issues, get issue detail, create issue, add comment, transition status, list projects |
| **GitHub** | List repositories, get repository, list issues, get issue |
| **Trello** | List boards, get board, list cards, get card, create card |

All responses are formatted as **Discord Embeds** with colour-coded results.

---

## Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 10.0+ |
| A Discord Application & Bot token | [Developer Portal](https://discord.com/developers/applications) |
| Jira Cloud account with API token | _(optional — commands will error gracefully if not configured)_ |
| Trello account with API key and token | _(optional)_ |
| GitHub Personal Access Token | _(optional)_ |

---

## Configuration

Copy `appsettings.example.json` to `appsettings.json` (or use environment variables):

```json
{
  "Discord": {
    "BotToken": "YOUR_DISCORD_BOT_TOKEN_HERE",
    "GuildId": null
  },
  "Jira": {
    "BaseUrl": "https://yourcompany.atlassian.net",
    "Email": "your-email@example.com",
    "ApiToken": "your-jira-api-token"
  },
  "Trello": {
    "ApiKey": "your-trello-api-key",
    "Token": "your-trello-oauth-token"
  },
  "GitHub": {
    "Token": "your-github-personal-access-token"
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

#### Jira API Token
1. Go to [id.atlassian.com/manage-profile/security/api-tokens](https://id.atlassian.com/manage-profile/security/api-tokens)
2. Click **Create API token**

#### Trello API Key and Token
1. Go to [trello.com/app-key](https://trello.com/app-key)
2. Copy the **API Key**
3. Click the **Token** link to generate a token

#### GitHub Personal Access Token
1. Go to [github.com/settings/tokens](https://github.com/settings/tokens)
2. Generate a **classic** token with `repo`, `read:org` scopes (or a fine-grained token)

---

## Running locally

```bash
# 1. Clone / navigate to the repository
cd Jira_Management

# 2. Copy and edit configuration
cp src/ProjectManagement.Discord/appsettings.example.json \
   src/ProjectManagement.Discord/appsettings.json
# Fill in your credentials in appsettings.json

# 3. Run the bot
dotnet run --project src/ProjectManagement.Discord/ProjectManagement.Discord.csproj
```

The bot connects to Discord and logs output to the console.  
You should see:
```
info: ProjectManagement.Discord.Bot.DiscordBotService[0]
      Discord bot connected successfully
info: ProjectManagement.Discord.Bot.InteractionHandler[0]
      Slash commands registered to guild 123456789
```

---

## Available Commands

All commands use Discord slash-command syntax (`/group subcommand [args]`).

### `/jira` — Jira commands

| Command | Parameters | Description |
|---|---|---|
| `/jira search` | `project_key` *(required)*, `status`, `issue_type` | Search issues with optional filters |
| `/jira get` | `issue_key` *(required)* | Get full details of an issue |
| `/jira create` | `project_key`, `summary` *(required)*, `issue_type`, `description`, `priority` | Create a new issue |
| `/jira comment` | `issue_key`, `text` *(required)* | Add a comment to an issue |
| `/jira transition` | `issue_key`, `transition_name` *(required)* | Move an issue to a new status |
| `/jira projects` | *(none)* | List all accessible projects |

### `/github` — GitHub commands

| Command | Parameters | Description |
|---|---|---|
| `/github repos` | *(none)* | List your repositories |
| `/github repo` | `owner`, `name` *(required)* | Get details of a repository |
| `/github issues` | `owner`, `repo` *(required)*, `state` (default: open) | List issues |
| `/github issue` | `owner`, `repo`, `number` *(required)* | Get details of an issue |

### `/trello` — Trello commands

| Command | Parameters | Description |
|---|---|---|
| `/trello boards` | *(none)* | List your boards |
| `/trello board` | `board_id` *(required)* | Get details of a board |
| `/trello cards` | `board_id` *(required)* | List cards on a board |
| `/trello card` | `card_id` *(required)* | Get details of a card |
| `/trello create-card` | `list_id`, `name` *(required)*, `description` | Create a new card |

---

## Architecture

See [`ARCHITECTURE.md`](ARCHITECTURE.md) for the full architecture document.

---

## Testing

```bash
# Run Discord bot tests only
dotnet test tests/ProjectManagement.Discord.Tests/

# Run all solution tests
dotnet test ProjectManagement.slnx

# Run with coverage report
dotnet test tests/ProjectManagement.Discord.Tests/ \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage
```

The Discord bot project achieves **≥ 86% line coverage** across all testable classes.

---

## Deployment

See [`DEPLOYMENT.md`](DEPLOYMENT.md) for Docker, cloud (Render, Railway, Azure), and service deployment instructions.
