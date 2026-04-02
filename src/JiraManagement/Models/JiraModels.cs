using System.Text.Json.Serialization;

namespace JiraManagement.Models;

public class JiraIssue
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("self")]
    public string Self { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public IssueFields Fields { get; set; } = new();
}

public class IssueFields
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public AdfDocument? Description { get; set; }

    [JsonPropertyName("status")]
    public NamedField Status { get; set; } = new();

    [JsonPropertyName("issuetype")]
    public NamedField IssueType { get; set; } = new();

    [JsonPropertyName("priority")]
    public NamedField? Priority { get; set; }

    [JsonPropertyName("assignee")]
    public JiraUser? Assignee { get; set; }

    [JsonPropertyName("reporter")]
    public JiraUser? Reporter { get; set; }

    [JsonPropertyName("created")]
    public DateTime? Created { get; set; }

    [JsonPropertyName("updated")]
    public DateTime? Updated { get; set; }

    [JsonPropertyName("comment")]
    public CommentCollection? Comment { get; set; }
}

public class NamedField
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class JiraUser
{
    [JsonPropertyName("accountId")]
    public string AccountId { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("emailAddress")]
    public string EmailAddress { get; set; } = string.Empty;
}

public class CommentCollection
{
    [JsonPropertyName("comments")]
    public List<JiraComment> Comments { get; set; } = [];

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class JiraComment
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public JiraUser Author { get; set; } = new();

    [JsonPropertyName("body")]
    public AdfDocument? Body { get; set; }

    [JsonPropertyName("created")]
    public DateTime Created { get; set; }

    [JsonPropertyName("updated")]
    public DateTime Updated { get; set; }
}

/// <summary>Atlassian Document Format node.</summary>
public class AdfDocument
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public List<AdfNode>? Content { get; set; }
}

public class AdfNode
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("content")]
    public List<AdfNode>? Content { get; set; }
}

public class SearchResult
{
    [JsonPropertyName("issues")]
    public List<JiraIssue> Issues { get; set; } = [];

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("startAt")]
    public int StartAt { get; set; }

    [JsonPropertyName("maxResults")]
    public int MaxResults { get; set; }
}

public class IssueTransition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class TransitionsResult
{
    [JsonPropertyName("transitions")]
    public List<IssueTransition> Transitions { get; set; } = [];
}

public class CreateIssueRequest
{
    public string ProjectKey { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string IssueType { get; set; } = "Task";
    public string? Description { get; set; }
    public string? Priority { get; set; }
    public string? AssigneeAccountId { get; set; }
}

public class UpdateIssueRequest
{
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public string? Priority { get; set; }
    public string? AssigneeAccountId { get; set; }
}

public class SearchIssuesRequest
{
    public string ProjectKey { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? IssueType { get; set; }
    public string? AssigneeEmail { get; set; }
    public int MaxResults { get; set; } = 50;
    public int StartAt { get; set; } = 0;
}
