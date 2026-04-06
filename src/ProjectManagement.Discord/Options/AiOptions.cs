namespace ProjectManagement.Discord.Options;

/// <summary>
/// Configuration settings for the LLM AI assistant.
/// Bind from the <c>Ai</c> section in <c>appsettings.json</c> or via environment variables.
/// </summary>
public sealed class AiOptions
{
    /// <summary>The configuration section name used to bind this class.</summary>
    public const string SectionName = "Ai";

    /// <summary>
    /// OpenAI API key.
    /// Set via <c>Ai:ApiKey</c> or <c>AI_API_KEY</c>.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// OpenAI model to use (e.g. <c>gpt-4o</c>, <c>gpt-4o-mini</c>).
    /// Defaults to <c>gpt-4o-mini</c>.
    /// </summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Base URL of the deployed Project Management REST API (e.g. <c>https://yourapp.onrender.com</c>).
    /// The LLM will call this API via tool functions to perform Jira, GitHub, and Trello operations.
    /// </summary>
    public string ApiBaseUrl { get; set; } = string.Empty;
}
