using System.Text.Json.Serialization;

namespace ProjectManagement.Core.Trello.Models;

public class TrelloBoard
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Desc { get; set; } = string.Empty;

    [JsonPropertyName("closed")]
    public bool Closed { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public class TrelloList
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("closed")]
    public bool Closed { get; set; }

    [JsonPropertyName("idBoard")]
    public string IdBoard { get; set; } = string.Empty;
}

public class TrelloCard
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Desc { get; set; } = string.Empty;

    [JsonPropertyName("closed")]
    public bool Closed { get; set; }

    [JsonPropertyName("idBoard")]
    public string IdBoard { get; set; } = string.Empty;

    [JsonPropertyName("idList")]
    public string IdList { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("due")]
    public DateTime? Due { get; set; }

    [JsonPropertyName("labels")]
    public List<TrelloLabel> Labels { get; set; } = [];
}

public class TrelloLabel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string? Color { get; set; }
}

public class CreateCardRequest
{
    public string IdList { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Desc { get; set; }
    public DateTime? Due { get; set; }
}

public class UpdateCardRequest
{
    public string? Name { get; set; }
    public string? Desc { get; set; }
    public string? IdList { get; set; }
    public DateTime? Due { get; set; }
    public bool? Closed { get; set; }
}
