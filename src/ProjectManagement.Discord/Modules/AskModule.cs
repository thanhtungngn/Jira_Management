using System.Diagnostics.CodeAnalysis;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using ProjectManagement.Discord.Services;

namespace ProjectManagement.Discord.Modules;

/// <summary>
/// Single slash command that accepts a free-form natural-language prompt and lets the LLM
/// decide which Jira, GitHub, or Trello operations to perform.
/// </summary>
/// <remarks>
/// This replaces the previous hard-coded <c>/jira</c>, <c>/github</c>, and <c>/trello</c>
/// command groups. Users can now type plain English, e.g.:
/// <list type="bullet">
///   <item>"Show all open bugs in the PROJ project"</item>
///   <item>"Create a high-priority task in PROJ called 'Fix login page'"</item>
///   <item>"List my GitHub repos"</item>
///   <item>"What cards are on board abc123?"</item>
/// </list>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class AskModule : InteractionModuleBase<SocketInteractionContext>
{
    // Discord message body limit is 2000 characters; leave a small buffer.
    private const int MaxResponseLength = 1990;

    private readonly ILlmChatService _llmService;
    private readonly ILogger<AskModule> _logger;

    /// <summary>Initialises the module with required services.</summary>
    public AskModule(ILlmChatService llmService, ILogger<AskModule> logger)
    {
        _llmService = llmService;
        _logger     = logger;
    }

    /// <summary>Ask the AI assistant anything about your Jira, GitHub, or Trello projects.</summary>
    [SlashCommand("ask", "Ask the AI assistant about your Jira, GitHub, or Trello projects")]
    public async Task AskAsync(
        [Summary("prompt", "Your question or command (e.g. 'Show open bugs in PROJ')")] string prompt)
    {
        // Defer immediately so we have up to 15 minutes to respond.
        await DeferAsync();
        _logger.LogDebug("LLM ask: {Prompt}", prompt);

        var response = await _llmService.AskAsync(prompt);

        // Truncate if the LLM response exceeds Discord's message length limit.
        if (response.Length > MaxResponseLength)
            response = response[..MaxResponseLength] + "…";

        await FollowupAsync(response);
    }
}
