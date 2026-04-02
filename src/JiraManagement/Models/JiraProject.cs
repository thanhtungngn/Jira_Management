using System.Text.Json.Serialization;

namespace JiraManagement.Models;

public class JiraProject
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("projectTypeKey")]
    public string ProjectTypeKey { get; set; } = string.Empty;

    [JsonPropertyName("style")]
    public string? Style { get; set; }
}

public class PagedProjects
{
    [JsonPropertyName("values")]
    public List<JiraProject> Values { get; set; } = [];

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
