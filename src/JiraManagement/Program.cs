using JiraManagement;
using JiraManagement.Models;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var baseUrl  = config["JIRA_BASE_URL"]  ?? string.Empty;
var email    = config["JIRA_EMAIL"]     ?? string.Empty;
var apiToken = config["JIRA_API_TOKEN"] ?? string.Empty;

if (string.IsNullOrWhiteSpace(baseUrl) ||
    string.IsNullOrWhiteSpace(email)   ||
    string.IsNullOrWhiteSpace(apiToken))
{
    Console.Error.WriteLine(
        "Error: Missing Jira credentials.\n" +
        "Set JIRA_BASE_URL, JIRA_EMAIL, and JIRA_API_TOKEN " +
        "as environment variables or in appsettings.json.");
    return 1;
}

var client = JiraClient.Create(baseUrl, email, apiToken);

if (args.Length == 0)
{
    PrintHelp();
    return 0;
}

try
{
    return args[0].ToLowerInvariant() switch
    {
        "projects" => await HandleProjectsAsync(client, args[1..]),
        "issues"   => await HandleIssuesAsync(client, args[1..]),
        "help"     => Help(),
        _          => Unknown(args[0]),
    };
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

// ── Command handlers ──────────────────────────────────────────────────────────

static async Task<int> HandleProjectsAsync(IJiraClient client, string[] args)
{
    var sub = args.Length > 0 ? args[0].ToLowerInvariant() : "list";
    switch (sub)
    {
        case "list":
        {
            var projects = await client.GetProjectsAsync();
            if (projects.Count == 0)
            {
                Console.WriteLine("No projects found.");
                return 0;
            }
            Console.WriteLine($"Found {projects.Count} project(s):\n");
            foreach (var p in projects)
                Console.WriteLine($"  [{p.Key}] {p.Name}  ({p.ProjectTypeKey})");
            return 0;
        }

        case "get" when args.Length >= 2:
        {
            var project = await client.GetProjectAsync(args[1]);
            Console.WriteLine($"Project : {project.Name}");
            Console.WriteLine($"  Key   : {project.Key}");
            Console.WriteLine($"  ID    : {project.Id}");
            Console.WriteLine($"  Type  : {project.ProjectTypeKey}");
            return 0;
        }

        default:
            Console.Error.WriteLine("Usage: jira-mgmt projects [list|get <KEY>]");
            return 1;
    }
}

static async Task<int> HandleIssuesAsync(IJiraClient client, string[] args)
{
    if (args.Length == 0)
    {
        Console.Error.WriteLine("Usage: jira-mgmt issues <subcommand> ...");
        return 1;
    }

    var sub = args[0].ToLowerInvariant();

    switch (sub)
    {
        case "list" when args.Length >= 2:
        {
            var req = new SearchIssuesRequest { ProjectKey = args[1] };
            ParseFlags(args[2..], req);
            var result = await client.SearchIssuesAsync(req);
            if (result.Issues.Count == 0)
            {
                Console.WriteLine("No issues found.");
                return 0;
            }
            Console.WriteLine($"Showing {result.Issues.Count} of {result.Total} issue(s) in {args[1]}:\n");
            foreach (var issue in result.Issues)
                PrintIssueSummary(issue);
            return 0;
        }

        case "get" when args.Length >= 2:
        {
            var issue = await client.GetIssueAsync(args[1]);
            var f = issue.Fields;
            Console.WriteLine($"Issue   : {issue.Key}");
            Console.WriteLine($"  Summary  : {f.Summary}");
            Console.WriteLine($"  Status   : {f.Status.Name}");
            Console.WriteLine($"  Type     : {f.IssueType.Name}");
            Console.WriteLine($"  Priority : {f.Priority?.Name ?? "None"}");
            Console.WriteLine($"  Assignee : {f.Assignee?.DisplayName ?? "Unassigned"}");
            Console.WriteLine($"  Reporter : {f.Reporter?.DisplayName ?? "Unknown"}");
            Console.WriteLine($"  Created  : {f.Created?.ToLocalTime():g}");
            Console.WriteLine($"  Updated  : {f.Updated?.ToLocalTime():g}");

            if (f.Comment?.Comments.Count > 0)
            {
                Console.WriteLine($"\n  Comments ({f.Comment.Total}):");
                foreach (var c in f.Comment.Comments.TakeLast(3))
                    Console.WriteLine($"    [{c.Created.ToLocalTime():g}] {c.Author.DisplayName}: {ExtractText(c.Body)}");
            }
            return 0;
        }

        case "create" when args.Length >= 2:
        {
            var req = new CreateIssueRequest { ProjectKey = args[1] };
            ParseCreateFlags(args[2..], req);
            if (string.IsNullOrWhiteSpace(req.Summary))
            {
                Console.Error.WriteLine("Error: --summary is required.");
                return 1;
            }
            var issue = await client.CreateIssueAsync(req);
            Console.WriteLine($"Created issue: {issue.Key}");
            return 0;
        }

        case "update" when args.Length >= 2:
        {
            var req = new UpdateIssueRequest();
            ParseUpdateFlags(args[2..], req);
            await client.UpdateIssueAsync(args[1], req);
            Console.WriteLine($"Updated issue: {args[1]}");
            return 0;
        }

        case "transition" when args.Length >= 3:
        {
            await client.TransitionIssueAsync(args[1], args[2]);
            Console.WriteLine($"Transitioned {args[1]} to \"{args[2]}\"");
            return 0;
        }

        case "comment" when args.Length >= 3:
        {
            await client.AddCommentAsync(args[1], args[2]);
            Console.WriteLine($"Comment added to {args[1]}");
            return 0;
        }

        default:
            Console.Error.WriteLine(
                "Usage:\n" +
                "  jira-mgmt issues list <PROJECT_KEY> [--status <s>] [--type <t>] [--assignee <email>] [--max <n>]\n" +
                "  jira-mgmt issues get <ISSUE_KEY>\n" +
                "  jira-mgmt issues create <PROJECT_KEY> --summary <text> [--type <t>] [--description <d>] [--priority <p>]\n" +
                "  jira-mgmt issues update <ISSUE_KEY> [--summary <s>] [--description <d>] [--priority <p>]\n" +
                "  jira-mgmt issues transition <ISSUE_KEY> <transition>\n" +
                "  jira-mgmt issues comment <ISSUE_KEY> <text>");
            return 1;
    }
}

// ── Formatting helpers ────────────────────────────────────────────────────────

static void PrintIssueSummary(JiraIssue issue)
{
    var f = issue.Fields;
    Console.WriteLine(
        $"  [{issue.Key}] {f.Summary}\n" +
        $"         Status: {f.Status.Name} | Type: {f.IssueType.Name} | " +
        $"Priority: {f.Priority?.Name ?? "None"} | Assignee: {f.Assignee?.DisplayName ?? "Unassigned"}");
}

static string ExtractText(AdfDocument? doc)
{
    if (doc is null) return string.Empty;
    var sb = new System.Text.StringBuilder();
    foreach (var block in doc.Content ?? [])
        foreach (var inline in block.Content ?? [])
            if (inline.Text is not null)
                sb.Append(inline.Text);
    return sb.ToString();
}

// ── Flag parsers ──────────────────────────────────────────────────────────────

static void ParseFlags(string[] args, SearchIssuesRequest req)
{
    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i].ToLowerInvariant())
        {
            case "--status"   when i + 1 < args.Length: req.Status       = args[++i]; break;
            case "--type"     when i + 1 < args.Length: req.IssueType    = args[++i]; break;
            case "--assignee" when i + 1 < args.Length: req.AssigneeEmail = args[++i]; break;
            case "--max"      when i + 1 < args.Length:
                if (int.TryParse(args[++i], out var m)) req.MaxResults = m;
                break;
        }
    }
}

static void ParseCreateFlags(string[] args, CreateIssueRequest req)
{
    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i].ToLowerInvariant())
        {
            case "--summary"     when i + 1 < args.Length: req.Summary     = args[++i]; break;
            case "--type"        when i + 1 < args.Length: req.IssueType   = args[++i]; break;
            case "--description" when i + 1 < args.Length: req.Description = args[++i]; break;
            case "--priority"    when i + 1 < args.Length: req.Priority    = args[++i]; break;
        }
    }
}

static void ParseUpdateFlags(string[] args, UpdateIssueRequest req)
{
    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i].ToLowerInvariant())
        {
            case "--summary"     when i + 1 < args.Length: req.Summary     = args[++i]; break;
            case "--description" when i + 1 < args.Length: req.Description = args[++i]; break;
            case "--priority"    when i + 1 < args.Length: req.Priority    = args[++i]; break;
        }
    }
}

static void PrintHelp()
{
    Console.WriteLine(
        """
        Jira Management CLI
        
        Usage:
          jira-mgmt projects list
          jira-mgmt projects get <PROJECT_KEY>
        
          jira-mgmt issues list <PROJECT_KEY> [--status <s>] [--type <t>] [--assignee <email>] [--max <n>]
          jira-mgmt issues get <ISSUE_KEY>
          jira-mgmt issues create <PROJECT_KEY> --summary <text> [--type <t>] [--description <d>] [--priority <p>]
          jira-mgmt issues update <ISSUE_KEY> [--summary <s>] [--description <d>] [--priority <p>]
          jira-mgmt issues transition <ISSUE_KEY> <transition>
          jira-mgmt issues comment <ISSUE_KEY> <text>
        
        Environment variables (or appsettings.json):
          JIRA_BASE_URL    Your Jira instance URL (e.g. https://yourcompany.atlassian.net)
          JIRA_EMAIL       Your Atlassian account email
          JIRA_API_TOKEN   Your Jira API token
        """);
}

static int Help()      { PrintHelp(); return 0; }
static int Unknown(string cmd) { Console.Error.WriteLine($"Unknown command: {cmd}. Run 'jira-mgmt help' for usage."); return 1; }
