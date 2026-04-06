namespace ProjectManagement.Discord.Services;

/// <summary>
/// Sends a natural-language prompt to an LLM and returns a human-readable reply.
/// The LLM uses tool functions to interact with Jira, GitHub, and Trello via the deployed REST API.
/// </summary>
public interface ILlmChatService
{
    /// <summary>
    /// Process a natural-language <paramref name="prompt"/> and return the LLM's response.
    /// </summary>
    Task<string> AskAsync(string prompt, CancellationToken ct = default);
}
