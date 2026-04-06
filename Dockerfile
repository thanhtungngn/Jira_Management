# ── Build stage ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Copy project files first so the restore layer is cached independently
COPY src/ProjectManagement.Core/ProjectManagement.Core.csproj   src/ProjectManagement.Core/
COPY src/ProjectManagement.Api/ProjectManagement.Api.csproj     src/ProjectManagement.Api/

RUN dotnet restore src/ProjectManagement.Api/ProjectManagement.Api.csproj

# Copy the full source and publish
COPY src/ProjectManagement.Core/ src/ProjectManagement.Core/
COPY src/ProjectManagement.Api/  src/ProjectManagement.Api/
RUN dotnet publish src/ProjectManagement.Api/ProjectManagement.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

# ── Runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Render injects PORT=10000 at runtime; ASPNETCORE_HTTP_PORTS picks it up.
# The default here is a fallback for local docker run.
ENV ASPNETCORE_HTTP_PORTS=10000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ProjectManagement.Api.dll"]
