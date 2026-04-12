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

    [Fact]
    public async Task CreateConfluencePageAsync_DelegatesToClient()
    {
        var clientMock = new Mock<IConfluenceClient>();
        clientMock
            .Setup(c => c.CreatePageAsync("ENG", "New Doc", "<p>Body</p>", "111"))
            .ReturnsAsync(new ConfluencePage { Id = "200", Title = "New Doc", Version = 1 });

        var tools = new ConfluenceTools(clientMock.Object, NullLogger<ConfluenceTools>.Instance);
        var result = await tools.CreateConfluencePageAsync("ENG", "New Doc", "<p>Body</p>", "111");

        Assert.Equal("200", result.Id);
        clientMock.Verify(c => c.CreatePageAsync("ENG", "New Doc", "<p>Body</p>", "111"), Times.Once);
    }

    [Fact]
    public async Task DeleteConfluencePageAsync_DelegatesToClient()
    {
        var clientMock = new Mock<IConfluenceClient>();
        clientMock.Setup(c => c.DeletePageAsync("12345")).Returns(Task.CompletedTask);

        var tools = new ConfluenceTools(clientMock.Object, NullLogger<ConfluenceTools>.Instance);
        await tools.DeleteConfluencePageAsync("12345");

        clientMock.Verify(c => c.DeletePageAsync("12345"), Times.Once);
    }

    [Fact]
    public async Task GetConfluenceChildrenAsync_DelegatesToClient()
    {
        var clientMock = new Mock<IConfluenceClient>();
        clientMock
            .Setup(c => c.GetChildrenAsync("12345", 25))
            .ReturnsAsync([new ConfluencePage { Id = "c1", Title = "Child" }]);

        var tools = new ConfluenceTools(clientMock.Object, NullLogger<ConfluenceTools>.Instance);
        var result = await tools.GetConfluenceChildrenAsync("12345", 25);

        Assert.Single(result);
        clientMock.Verify(c => c.GetChildrenAsync("12345", 25), Times.Once);
    }

    [Fact]
    public async Task MoveConfluencePageAsync_DelegatesToClient()
    {
        var clientMock = new Mock<IConfluenceClient>();
        clientMock
            .Setup(c => c.MovePageAsync("12345", "999", true))
            .ReturnsAsync(new ConfluencePage { Id = "12345", ParentId = "999", Version = 2 });

        var tools = new ConfluenceTools(clientMock.Object, NullLogger<ConfluenceTools>.Instance);
        var result = await tools.MoveConfluencePageAsync("12345", "999", true);

        Assert.Equal("999", result.ParentId);
        clientMock.Verify(c => c.MovePageAsync("12345", "999", true), Times.Once);
    }
}
