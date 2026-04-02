# Jira Management

A .NET 8 console CLI tool to manage your Jira projects and issues from the command line.

## Features

- **Projects**: list all accessible projects, or get details for a specific project
- **Issues**: list, get, create, update, transition, and comment on issues
- Filters for issue listing: status, issue type, assignee, and result count
- Configuration via environment variables or `appsettings.json`
- No external HTTP libraries — built on `System.Net.Http` and `System.Text.Json`

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A Jira Cloud account with an [API token](https://id.atlassian.com/manage-profile/security/api-tokens)

## Setup

### 1. Clone the repository

```bash
git clone https://github.com/thanhtungngn/Jira_Management.git
cd Jira_Management
```

### 2. Configure credentials

Copy the example settings file and fill in your own values:

```bash
cp appsettings.example.json src/JiraManagement/appsettings.json
```

Edit `src/JiraManagement/appsettings.json`:

```json
{
  "JIRA_BASE_URL": "https://yourcompany.atlassian.net",
  "JIRA_EMAIL": "your-email@example.com",
  "JIRA_API_TOKEN": "your-api-token-here"
}
```

> **Tip:** You can also provide the three values as plain environment variables instead of the JSON file.

### 3. Build

```bash
dotnet build JiraManagement.slnx
```

## Usage

Run from the project directory:

```bash
cd src/JiraManagement
dotnet run -- <command>
```

Or build and run the binary:

```bash
dotnet build -c Release
./src/JiraManagement/bin/Release/net8.0/JiraManagement help
```

### Commands

#### Projects

```bash
# List all projects
dotnet run -- projects list

# Get details of a project
dotnet run -- projects get MYPROJ
```

#### Issues

```bash
# List issues in a project
dotnet run -- issues list MYPROJ
dotnet run -- issues list MYPROJ --status "In Progress"
dotnet run -- issues list MYPROJ --type Bug --max 20
dotnet run -- issues list MYPROJ --assignee dev@example.com

# Get full details of an issue
dotnet run -- issues get MYPROJ-42

# Create a new issue
dotnet run -- issues create MYPROJ --summary "Fix login bug" --type Bug --priority High
dotnet run -- issues create MYPROJ --summary "Add dark mode" --type Story --description "Users want a dark theme"

# Update an issue
dotnet run -- issues update MYPROJ-42 --summary "Updated title" --priority Medium

# Transition an issue (change its status)
dotnet run -- issues transition MYPROJ-42 "In Progress"
dotnet run -- issues transition MYPROJ-42 Done

# Add a comment
dotnet run -- issues comment MYPROJ-42 "Investigated and found root cause."
```

## Running Tests

```bash
dotnet test JiraManagement.slnx
```

## Project Structure

```
JiraManagement.slnx
├── src/
│   └── JiraManagement/
│       ├── JiraManagement.csproj
│       ├── Program.cs            # CLI entry point
│       ├── IJiraClient.cs        # Client interface
│       ├── JiraClient.cs         # Jira REST API v3 implementation
│       └── Models/
│           ├── JiraProject.cs
│           └── JiraModels.cs     # Issues, comments, ADF, requests
└── tests/
    └── JiraManagement.Tests/
        ├── JiraManagement.Tests.csproj
        └── JiraClientTests.cs    # Unit tests with mocked HTTP handler
```

## Configuration Reference

| Key              | Description                                        | Example                              |
|------------------|----------------------------------------------------|--------------------------------------|
| `JIRA_BASE_URL`  | Base URL of your Jira Cloud instance               | `https://yourcompany.atlassian.net`  |
| `JIRA_EMAIL`     | Atlassian account email used for authentication    | `you@example.com`                    |
| `JIRA_API_TOKEN` | API token generated from Atlassian account settings | `ATATxxxxxxxx`                      |
