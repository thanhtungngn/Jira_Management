namespace ProjectManagement.Core.Confluence.Models;

public class ConfluencePage
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? SpaceKey { get; set; }
    public string? ParentId { get; set; }
    public string? BodyStorageValue { get; set; }
}
