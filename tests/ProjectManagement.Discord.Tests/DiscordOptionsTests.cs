using ProjectManagement.Discord.Options;

namespace ProjectManagement.Discord.Tests;

/// <summary>
/// Unit tests for <see cref="DiscordOptions"/>.
/// Verifies that default values and property assignment behave as expected.
/// </summary>
public class DiscordOptionsTests
{
    [Fact]
    public void DefaultValues_AreEmptyOrNull()
    {
        var opts = new DiscordOptions();

        Assert.Equal(string.Empty, opts.BotToken);
        Assert.Null(opts.GuildId);
    }

    [Fact]
    public void SectionName_IsDiscord()
    {
        Assert.Equal("Discord", DiscordOptions.SectionName);
    }

    [Fact]
    public void BotToken_CanBeSet()
    {
        var opts = new DiscordOptions { BotToken = "my-token" };

        Assert.Equal("my-token", opts.BotToken);
    }

    [Fact]
    public void GuildId_CanBeSet()
    {
        var opts = new DiscordOptions { GuildId = 123456789UL };

        Assert.Equal(123456789UL, opts.GuildId);
    }
}
