using ProjectManagement.Core.Confluence.Models;

namespace ProjectManagement.Core.Confluence;

public interface IConfluenceClient
{
    Task<ConfluencePage> UpdatePageAsync(string pageId, string bodyStorageValue, string? title = null, bool minorEdit = false);
}
