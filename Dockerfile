# syntax=docker/dockerfile:1
# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file first for layer-cached package restore.
COPY src/ProjectManagement.Discord/ProjectManagement.Discord.csproj src/ProjectManagement.Discord/

RUN dotnet restore src/ProjectManagement.Discord/ProjectManagement.Discord.csproj

# Copy the rest of the source code.
COPY src/ProjectManagement.Discord/ src/ProjectManagement.Discord/

# Publish a Release build.
RUN dotnet publish src/ProjectManagement.Discord/ProjectManagement.Discord.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final

# Run as a non-root user for security.
RUN adduser --disabled-password --no-create-home --uid 1001 pm-discord
USER pm-discord

WORKDIR /app
COPY --from=build /app/publish .

# The bot uses outbound TCP only (Discord gateway port 443/WSS).
# No inbound ports need to be exposed.

ENTRYPOINT ["dotnet", "ProjectManagement.Discord.dll"]
