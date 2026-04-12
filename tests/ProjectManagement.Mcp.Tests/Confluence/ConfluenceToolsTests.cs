using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProjectManagement.Core.Confluence;
using ProjectManagement.Core.Confluence.Models;
using ProjectManagement.Mcp.Confluence;

namespace ProjectManagement.Mcp.Tests.Confluence;

public class ConfluenceToolsTests
{
    [Fact]
    public async Task UpdateConfluenceDocumentAsync_DelegatesToClient()
    {
        var clientMock = new Mock<IConfluenceClient>();
        clientMock
            .Setup(c => c.UpdatePageAsync("12345", "<p>Hello</p>", "Doc Title", true))
            .ReturnsAsync(new ConfluencePage { Id = "12345", Title = "Doc Title", Version = 4 });

        var tools = new ConfluenceTools(clientMock.Object, NullLogger<ConfluenceTools>.Instance);

        var result = await tools.UpdateConfluenceDocumentAsync("12345", "<p>Hello</p>", "Doc Title", true);

        Assert.Equal("12345", result.Id);
        Assert.Equal("Doc Title", result.Title);
        Assert.Equal(4, result.Version);

        clientMock.Verify(c => c.UpdatePageAsync("12345", "<p>Hello</p>", "Doc Title", true), Times.Once);
    }
}
