using ProjectManagement.Core.Confluence.Models;

namespace ProjectManagement.Core.Confluence;

public interface IConfluenceClient
{
    Task<ConfluencePage> GetPageAsync(string pageId);
    Task<List<ConfluencePage>> GetChildrenAsync(string pageId, int limit = 50);
    Task<ConfluencePage> CreatePageAsync(string spaceKey, string title, string bodyStorageValue, string? parentPageId = null);
    Task<ConfluencePage> CreateFolderAsync(string spaceKey, string title, string? parentPageId = null);
    Task<ConfluencePage> UpdatePageAsync(string pageId, string bodyStorageValue, string? title = null, bool minorEdit = false);
    Task<ConfluencePage> MovePageAsync(string pageId, string newParentPageId, bool minorEdit = false);
    Task DeletePageAsync(string pageId);
}
