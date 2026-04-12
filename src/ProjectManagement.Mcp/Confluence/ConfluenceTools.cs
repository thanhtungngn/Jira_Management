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
}
