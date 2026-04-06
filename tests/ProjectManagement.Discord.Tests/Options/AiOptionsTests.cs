using ProjectManagement.Discord.Options;

namespace ProjectManagement.Discord.Tests.Options;

/// <summary>
/// Unit tests for <see cref="AiOptions"/>.
/// </summary>
public class AiOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var opts = new AiOptions();

        Assert.Equal(string.Empty, opts.ApiKey);
        Assert.Equal("gpt-4o-mini", opts.Model);
        Assert.Equal(string.Empty, opts.ApiBaseUrl);
    }

    [Fact]
    public void SectionName_IsAi()
    {
        Assert.Equal("Ai", AiOptions.SectionName);
    }

    [Fact]
    public void ApiKey_CanBeSet()
    {
        var opts = new AiOptions { ApiKey = "sk-test-key" };

        Assert.Equal("sk-test-key", opts.ApiKey);
    }

    [Fact]
    public void Model_CanBeSet()
    {
        var opts = new AiOptions { Model = "gpt-4o" };

        Assert.Equal("gpt-4o", opts.Model);
    }

    [Fact]
    public void ApiBaseUrl_CanBeSet()
    {
        var opts = new AiOptions { ApiBaseUrl = "https://myapp.onrender.com" };

        Assert.Equal("https://myapp.onrender.com", opts.ApiBaseUrl);
    }
}
