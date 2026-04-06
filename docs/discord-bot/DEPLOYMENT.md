# Discord Bot — Deployment Guide

## Overview

The Discord bot is a .NET 10 Worker Service (console application). It can be deployed:

- **Locally** (development / personal use)
- **Docker container** (recommended for self-hosting)
- **Render** (free/paid, same platform as the other services in this solution)
- **Railway** (free tier available)
- **Azure Container Apps / App Service**
- **Systemd service** (Linux VPS)

The bot needs outbound internet access (to Discord gateway WSS endpoints and to Jira/Trello/GitHub APIs). It does **not** need any inbound ports.

---

## 1. Local / Development

```bash
# Copy the example settings and fill in your credentials
cp src/ProjectManagement.Discord/appsettings.example.json \
   src/ProjectManagement.Discord/appsettings.json

# Edit appsettings.json with your tokens

# Run
dotnet run --project src/ProjectManagement.Discord
```

> **Tip:** Set `Discord:GuildId` to your development server ID so slash commands register instantly (within seconds) rather than globally (up to 1 hour).

---

## 2. Docker

### Build the image

A `Dockerfile.discord` is provided at the repository root.

```dockerfile
# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/ProjectManagement.Discord/ProjectManagement.Discord.csproj \
    -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProjectManagement.Discord.dll"]
```

```bash
# Build
docker build -f Dockerfile.discord -t pm-discord-bot .

# Run (pass secrets via environment variables — never bake them into the image)
docker run -d \
  -e Discord__BotToken="YOUR_TOKEN" \
  -e Discord__GuildId="YOUR_GUILD_ID" \
  -e Jira__BaseUrl="https://yourco.atlassian.net" \
  -e Jira__Email="you@example.com" \
  -e Jira__ApiToken="YOUR_JIRA_TOKEN" \
  -e Trello__ApiKey="YOUR_TRELLO_KEY" \
  -e Trello__Token="YOUR_TRELLO_TOKEN" \
  -e GitHub__Token="YOUR_GITHUB_TOKEN" \
  --name pm-discord-bot \
  pm-discord-bot
```

### Docker Compose

Add to your existing `docker-compose.yml`:

```yaml
  discord-bot:
    build:
      context: .
      dockerfile: Dockerfile.discord
    restart: unless-stopped
    environment:
      - Discord__BotToken=${DISCORD_BOT_TOKEN}
      - Discord__GuildId=${DISCORD_GUILD_ID}
      - Jira__BaseUrl=${JIRA_BASE_URL}
      - Jira__Email=${JIRA_EMAIL}
      - Jira__ApiToken=${JIRA_API_TOKEN}
      - Trello__ApiKey=${TRELLO_API_KEY}
      - Trello__Token=${TRELLO_TOKEN}
      - GitHub__Token=${GITHUB_TOKEN}
```

---

## 3. Render

[Render](https://render.com) supports Docker-based background workers.

1. Create a new **Background Worker** service in Render.
2. Set **Docker** as the runtime and point to your `Dockerfile.discord`.
3. Add the following environment variables in the Render dashboard:

| Key | Value |
|---|---|
| `Discord__BotToken` | Your bot token |
| `Discord__GuildId` | *(optional)* Your server ID |
| `Jira__BaseUrl` | `https://yourco.atlassian.net` |
| `Jira__Email` | Your Jira email |
| `Jira__ApiToken` | Your Jira API token |
| `Trello__ApiKey` | Your Trello API key |
| `Trello__Token` | Your Trello token |
| `GitHub__Token` | Your GitHub PAT |

4. Deploy. Render will build and run the container.

> **Free tier note:** Render's free tier spins down idle services. A Discord bot should stay alive as long as it's connected to the gateway. Background workers on paid plans run indefinitely.

---

## 4. Railway

1. Create a new **Service** in your Railway project.
2. Connect your GitHub repository.
3. Set the **Build Command** to:
   ```
   dotnet publish src/ProjectManagement.Discord -c Release -o /app
   ```
4. Set the **Start Command** to:
   ```
   dotnet /app/ProjectManagement.Discord.dll
   ```
5. Add environment variables (same as Render above).

---

## 5. Azure Container Apps

```bash
# Create a resource group and container app environment
az group create --name rg-discord-bot --location eastus
az containerapp env create --name cae-discord --resource-group rg-discord-bot --location eastus

# Push your image to Azure Container Registry (or Docker Hub)
az acr build --registry myregistry --image pm-discord-bot:latest \
  --file Dockerfile.discord .

# Deploy
az containerapp create \
  --name pm-discord-bot \
  --resource-group rg-discord-bot \
  --environment cae-discord \
  --image myregistry.azurecr.io/pm-discord-bot:latest \
  --env-vars \
    Discord__BotToken=secretref:discord-bot-token \
    Jira__BaseUrl=https://yourco.atlassian.net \
    Jira__Email=you@example.com \
    Jira__ApiToken=secretref:jira-api-token \
    GitHub__Token=secretref:github-token \
  --min-replicas 1 \
  --max-replicas 1
```

---

## 6. Linux Systemd service

```bash
# Publish self-contained binary
dotnet publish src/ProjectManagement.Discord \
  -c Release -r linux-x64 --self-contained \
  -o /opt/pm-discord-bot

# Create a systemd unit file
cat > /etc/systemd/system/pm-discord-bot.service <<EOF
[Unit]
Description=ProjectManagement Discord Bot
After=network.target

[Service]
Type=simple
User=pm-bot
WorkingDirectory=/opt/pm-discord-bot
ExecStart=/opt/pm-discord-bot/ProjectManagement.Discord
Restart=always
RestartSec=10
Environment=Discord__BotToken=YOUR_TOKEN
Environment=Jira__BaseUrl=https://yourco.atlassian.net
Environment=Jira__Email=you@example.com
Environment=Jira__ApiToken=YOUR_JIRA_TOKEN
Environment=GitHub__Token=YOUR_GITHUB_TOKEN

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable pm-discord-bot
systemctl start pm-discord-bot
systemctl status pm-discord-bot
```

---

## Environment Variable Reference

.NET's configuration system maps double-underscore (`__`) to the nested JSON structure separator (`:`) in environment variables.

| Environment Variable | `appsettings.json` Key | Required |
|---|---|---|
| `Discord__BotToken` | `Discord:BotToken` | ✅ Yes |
| `Discord__GuildId` | `Discord:GuildId` | No (global commands if omitted) |
| `Jira__BaseUrl` | `Jira:BaseUrl` | Only if using Jira commands |
| `Jira__Email` | `Jira:Email` | Only if using Jira commands |
| `Jira__ApiToken` | `Jira:ApiToken` | Only if using Jira commands |
| `Trello__ApiKey` | `Trello:ApiKey` | Only if using Trello commands |
| `Trello__Token` | `Trello:Token` | Only if using Trello commands |
| `GitHub__Token` | `GitHub:Token` | Only if using GitHub commands |

> **Security:** Never commit secrets to source control. Use environment variables, Docker secrets, or a secrets manager (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault).

---

## Health & Monitoring

The Discord bot is a stateful TCP connection to Discord's gateway servers. Monitoring options:

- **Logs** — the bot logs connection events, command executions, and errors to stdout; pipe to your preferred log aggregator (Datadog, Splunk, Loki, CloudWatch).
- **Restart policy** — configure `Restart=always` (systemd) or `restart: unless-stopped` (Docker) so the bot reconnects automatically after crashes.
- **Gateway reconnection** — Discord.Net handles gateway reconnection automatically with exponential backoff.

---

## Upgrading

```bash
# Pull latest changes
git pull

# Re-publish / rebuild Docker image
docker build -f Dockerfile.discord -t pm-discord-bot:new .

# Swap the running container
docker stop pm-discord-bot
docker rm pm-discord-bot
docker run -d ... --name pm-discord-bot pm-discord-bot:new
```

Slash commands are registered on bot startup; re-deploying will re-register any added/removed commands automatically.
