using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using ProjectManagement.Core.Confluence;
using ProjectManagement.Core.Confluence.Models;

namespace ProjectManagement.Mcp.Confluence;

[McpServerToolType]
public sealed class ConfluenceTools
{
    private readonly IConfluenceClient _client;
    private readonly ILogger<ConfluenceTools> _logger;

    public ConfluenceTools(IConfluenceClient client, ILogger<ConfluenceTools> logger)
    {
        _client = client;
        _logger = logger;
    }

    [McpServerTool(Name = "update_confluence_document"), Description("Update an existing Confluence page by page ID using storage-format content.")]
    public async Task<ConfluencePage> UpdateConfluenceDocumentAsync(
        string pageId,
        string content,
        string? title = null,
        bool minorEdit = false)
    {
        _logger.LogInformation("[MCP] update_confluence_document: {PageId}", pageId);
        return await _client.UpdatePageAsync(pageId, content, title, minorEdit);
    }

    [McpServerTool(Name = "create_confluence_page"), Description("Create a Confluence page in a space, optionally under a parent page.")]
    public async Task<ConfluencePage> CreateConfluencePageAsync(
        string spaceKey,
        string title,
        string content,
        string? parentPageId = null)
    {
        _logger.LogInformation("[MCP] create_confluence_page: space={SpaceKey} title={Title}", spaceKey, title);
        return await _client.CreatePageAsync(spaceKey, title, content, parentPageId);
    }

    [McpServerTool(Name = "create_confluence_folder"), Description("Create a Confluence folder-like container page in a space, optionally under a parent page.")]
    public async Task<ConfluencePage> CreateConfluenceFolderAsync(
        string spaceKey,
        string title,
        string? parentPageId = null)
    {
        _logger.LogInformation("[MCP] create_confluence_folder: space={SpaceKey} title={Title}", spaceKey, title);
        return await _client.CreateFolderAsync(spaceKey, title, parentPageId);
    }

    [McpServerTool(Name = "get_confluence_page"), Description("Get details of a Confluence page by page ID.")]
    public async Task<ConfluencePage> GetConfluencePageAsync(string pageId)
    {
        _logger.LogInformation("[MCP] get_confluence_page: {PageId}", pageId);
        return await _client.GetPageAsync(pageId);
    }

    [McpServerTool(Name = "get_confluence_children"), Description("List child pages under a Confluence page to inspect structure.")]
    public async Task<List<ConfluencePage>> GetConfluenceChildrenAsync(string pageId, int limit = 50)
    {
        _logger.LogInformation("[MCP] get_confluence_children: {PageId}", pageId);
        return await _client.GetChildrenAsync(pageId, limit);
    }

    [McpServerTool(Name = "move_confluence_page"), Description("Move a Confluence page under a new parent page.")]
    public async Task<ConfluencePage> MoveConfluencePageAsync(string pageId, string newParentPageId, bool minorEdit = false)
    {
        _logger.LogInformation("[MCP] move_confluence_page: page={PageId} parent={ParentId}", pageId, newParentPageId);
        return await _client.MovePageAsync(pageId, newParentPageId, minorEdit);
    }

    [McpServerTool(Name = "delete_confluence_page"), Description("Delete a Confluence page by page ID.")]
    public async Task DeleteConfluencePageAsync(string pageId)
    {
        _logger.LogInformation("[MCP] delete_confluence_page: {PageId}", pageId);
        await _client.DeletePageAsync(pageId);
    }
}
