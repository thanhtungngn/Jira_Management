using ProjectManagement.Core.Jira.Models;
using ProjectManagement.Discord.Formatting;

namespace ProjectManagement.Discord.Tests.Formatting;

/// <summary>
/// Unit tests for <see cref="JiraEmbedBuilder"/>.
/// These are pure-function tests — no mocking required.
/// </summary>
public class JiraEmbedBuilderTests
{
    // ── BuildIssueList ─────────────────────────────────────────────────────────

    [Fact]
    public void BuildIssueList_WithIssues_IncludesProjectKeyInTitle()
    {
        var result = new SearchResult
        {
            Total  = 1,
            Issues = [new JiraIssue { Key = "PROJ-1", Fields = new IssueFields { Summary = "Bug" } }],
        };

        var embed = JiraEmbedBuilder.BuildIssueList(result, "PROJ");

        Assert.Contains("PROJ", embed.Title);
        Assert.Single(embed.Fields);
    }

    [Fact]
    public void BuildIssueList_EmptyResults_ShowsNoIssuesMessage()
    {
        var result = new SearchResult { Total = 0, Issues = [] };

        var embed = JiraEmbedBuilder.BuildIssueList(result, "PROJ");

        Assert.Contains("No issues", embed.Description ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildIssueList_MoreThanTenIssues_ShowsFirstTenAndTruncationNote()
    {
        var issues = Enumerable.Range(1, 12)
            .Select(i => new JiraIssue
            {
                Key    = $"PROJ-{i}",
                Fields = new IssueFields { Summary = $"Issue {i}" },
            })
            .ToList();

        var result = new SearchResult { Total = 12, Issues = issues };

        var embed = JiraEmbedBuilder.BuildIssueList(result, "PROJ");

        // Only 10 fields should be rendered.
        Assert.Equal(10, embed.Fields.Length);
        Assert.Contains("10", embed.Description ?? "");
    }

    // ── BuildIssueDetail ───────────────────────────────────────────────────────

    [Fact]
    public void BuildIssueDetail_FullIssue_IncludesKeyAndSummary()
    {
        var issue = new JiraIssue
        {
            Key    = "PROJ-99",
            Fields = new IssueFields
            {
                Summary   = "Detailed issue",
                Status    = new NamedField { Name = "Done" },
                IssueType = new NamedField { Name = "Bug" },
                Priority  = new NamedField { Name = "High" },
                Assignee  = new JiraUser { DisplayName = "Alice" },
                Reporter  = new JiraUser { DisplayName = "Bob" },
                Description = new AdfDocument
                {
                    Type    = "doc",
                    Version = 1,
                    Content =
                    [
                        new AdfNode
                        {
                            Type    = "paragraph",
                            Content = [new AdfNode { Type = "text", Text = "Some description text" }],
                        },
                    ],
                },
                Comment = new CommentCollection
                {
                    Total    = 1,
                    Comments =
                    [
                        new JiraComment
                        {
                            Author  = new JiraUser { DisplayName = "Charlie" },
                            Created = DateTime.UtcNow,
                            Body    = new AdfDocument { Type = "doc", Version = 1 },
                        },
                    ],
                },
            },
        };

        var embed = JiraEmbedBuilder.BuildIssueDetail(issue);

        Assert.Contains("PROJ-99", embed.Title);
        Assert.Contains("Detailed issue", embed.Title);
        // Status, Type, Priority, Assignee, Reporter fields should be present.
        Assert.True(embed.Fields.Length >= 5);
    }

    [Fact]
    public void BuildIssueDetail_NoDescription_DoesNotAddDescriptionField()
    {
        var issue = new JiraIssue
        {
            Key    = "PROJ-1",
            Fields = new IssueFields { Summary = "No desc", Status = new NamedField() },
        };

        var embed = JiraEmbedBuilder.BuildIssueDetail(issue);

        Assert.DoesNotContain(embed.Fields, f => f.Name == "Description");
    }

    // ── BuildCreated ───────────────────────────────────────────────────────────

    [Fact]
    public void BuildCreated_ContainsKeyAndSummary()
    {
        var issue = new JiraIssue
        {
            Key    = "PROJ-55",
            Fields = new IssueFields { Summary = "Created task", IssueType = new NamedField { Name = "Task" }, Status = new NamedField { Name = "To Do" } },
        };

        var embed = JiraEmbedBuilder.BuildCreated(issue);

        Assert.Contains("Created", embed.Title, StringComparison.OrdinalIgnoreCase);
        Assert.True(embed.Fields.Any(f => f.Value.Contains("PROJ-55")));
    }

    // ── BuildCommentAdded ──────────────────────────────────────────────────────

    [Fact]
    public void BuildCommentAdded_ContainsIssueKey()
    {
        var embed = JiraEmbedBuilder.BuildCommentAdded("PROJ-77");

        Assert.Contains("Comment", embed.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PROJ-77", embed.Description ?? "");
    }

    // ── BuildTransitioned ──────────────────────────────────────────────────────

    [Fact]
    public void BuildTransitioned_ContainsKeyAndTransitionName()
    {
        var embed = JiraEmbedBuilder.BuildTransitioned("PROJ-3", "In Progress");

        Assert.Contains("Transition", embed.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PROJ-3",     embed.Description ?? "");
        Assert.Contains("In Progress", embed.Description ?? "");
    }

    // ── BuildProjectList ───────────────────────────────────────────────────────

    [Fact]
    public void BuildProjectList_WithProjects_RendersFields()
    {
        var projects = new List<JiraProject>
        {
            new() { Key = "A", Name = "Project A" },
            new() { Key = "B", Name = "Project B" },
        };

        var embed = JiraEmbedBuilder.BuildProjectList(projects);

        Assert.Equal(2, embed.Fields.Length);
    }

    [Fact]
    public void BuildProjectList_EmptyList_ShowsNoProjectsMessage()
    {
        var embed = JiraEmbedBuilder.BuildProjectList([]);

        Assert.Contains("No projects", embed.Description ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildProjectList_MoreThanFifteenProjects_ShowsFirstFifteen()
    {
        var projects = Enumerable.Range(1, 20)
            .Select(i => new JiraProject { Key = $"P{i}", Name = $"Project {i}" })
            .ToList();

        var embed = JiraEmbedBuilder.BuildProjectList(projects);

        Assert.Equal(15, embed.Fields.Length);
    }

    // ── BuildError ─────────────────────────────────────────────────────────────

    [Fact]
    public void BuildError_ContainsTitleAndMessage()
    {
        var embed = JiraEmbedBuilder.BuildError("Test Error", "Something went wrong");

        Assert.Contains("Test Error",          embed.Title);
        Assert.Contains("Something went wrong", embed.Description ?? "");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null,  5, "")]
    [InlineData("",    5, "")]
    [InlineData("abc", 5, "abc")]
    [InlineData("abcdefgh", 5, "abcde…")]
    public void Truncate_HandlesVariousInputs(string? input, int max, string expected)
    {
        Assert.Equal(expected, JiraEmbedBuilder.Truncate(input, max));
    }

    [Fact]
    public void ExtractAdfText_NullDocument_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, JiraEmbedBuilder.ExtractAdfText(null));
    }

    [Fact]
    public void ExtractAdfText_NestedNodes_ExtractsAllText()
    {
        var doc = new AdfDocument
        {
            Type    = "doc",
            Version = 1,
            Content =
            [
                new AdfNode
                {
                    Type    = "paragraph",
                    Content =
                    [
                        new AdfNode { Type = "text", Text = "Hello" },
                        new AdfNode { Type = "text", Text = "World" },
                    ],
                },
            ],
        };

        var text = JiraEmbedBuilder.ExtractAdfText(doc);

        Assert.Contains("Hello", text);
        Assert.Contains("World", text);
    }
}
