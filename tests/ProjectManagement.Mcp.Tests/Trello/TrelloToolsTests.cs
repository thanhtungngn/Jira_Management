using Moq;
using ProjectManagement.Core.Trello;
using ProjectManagement.Core.Trello.Models;
using ProjectManagement.Mcp.Trello;

namespace ProjectManagement.Mcp.Tests.Trello;

public class TrelloToolsTests
{
    private readonly Mock<ITrelloClient> _clientMock = new();
    private readonly TrelloTools _tools;

    public TrelloToolsTests()
    {
        _tools = new TrelloTools(_clientMock.Object);
    }

    [Fact]
    public async Task GetBoardsAsync_DelegatesToClient()
    {
        var expected = new List<TrelloBoard> { new() { Id = "b1", Name = "Sprint Board" } };
        _clientMock.Setup(c => c.GetBoardsAsync()).ReturnsAsync(expected);

        var result = await _tools.GetBoardsAsync();

        Assert.Single(result);
        Assert.Equal("Sprint Board", result[0].Name);
    }

    [Fact]
    public async Task GetBoardAsync_DelegatesToClient()
    {
        var expected = new TrelloBoard { Id = "b1", Name = "Sprint Board" };
        _clientMock.Setup(c => c.GetBoardAsync("b1")).ReturnsAsync(expected);

        var result = await _tools.GetBoardAsync("b1");

        Assert.Equal("b1", result.Id);
    }

    [Fact]
    public async Task GetCardsAsync_DelegatesToClient()
    {
        var expected = new List<TrelloCard> { new() { Id = "c1", Name = "Fix bug" } };
        _clientMock.Setup(c => c.GetCardsAsync("b1")).ReturnsAsync(expected);

        var result = await _tools.GetCardsAsync("b1");

        Assert.Single(result);
        Assert.Equal("Fix bug", result[0].Name);
    }

    [Fact]
    public async Task GetCardAsync_DelegatesToClient()
    {
        var expected = new TrelloCard { Id = "c1", Name = "Fix bug" };
        _clientMock.Setup(c => c.GetCardAsync("c1")).ReturnsAsync(expected);

        var result = await _tools.GetCardAsync("c1");

        Assert.Equal("c1", result.Id);
    }

    [Fact]
    public async Task CreateCardAsync_DelegatesToClient()
    {
        var expected = new TrelloCard { Id = "c2", Name = "New Task" };
        _clientMock
            .Setup(c => c.CreateCardAsync(It.Is<CreateCardRequest>(r =>
                r.IdList == "list1" && r.Name == "New Task")))
            .ReturnsAsync(expected);

        var result = await _tools.CreateCardAsync("list1", "New Task");

        Assert.Equal("c2", result.Id);
    }

    [Fact]
    public async Task DeleteCardAsync_DelegatesToClient()
    {
        _clientMock.Setup(c => c.DeleteCardAsync("c1")).Returns(Task.CompletedTask);

        await _tools.DeleteCardAsync("c1");

        _clientMock.Verify(c => c.DeleteCardAsync("c1"), Times.Once);
    }
}
