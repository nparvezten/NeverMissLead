using Microsoft.Extensions.Configuration;
using NeverMissLead.Application.Interfaces;
using OpenAI.Chat;

namespace NeverMissLead.Infrastructure.Ai;

/// <summary>
/// Client-ready LLM provider using the official OpenAI SDK.
/// </summary>
public sealed class OpenAiLlmClient : ILlmClient
{
    private readonly ChatClient _chatClient;

    public OpenAiLlmClient(IConfiguration configuration)
    {
        var apiKey = configuration["AiProviders:OpenAi:ApiKey"] ?? string.Empty;
        var model = configuration["AiProviders:OpenAi:Model"] ?? "gpt-4o-mini";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is missing. Set 'AiProviders:OpenAi:ApiKey' in appsettings.Local.json or via the OPENAI_API_KEY environment variable.");
        }

        _chatClient = new ChatClient(model, apiKey);
    }

    public async Task<LlmResponse?> GenerateAsync(
        string systemPrompt,
        string userQuestion,
        IReadOnlyList<RetrievedChunk> contextChunks,
        CancellationToken cancellationToken = default)
    {
        if (contextChunks == null || contextChunks.Count == 0)
            return null;

        var populatedSystemPrompt = systemPrompt.Contains("{0}")
            ? LlmPromptHelper.BuildSystemPrompt(contextChunks)
            : systemPrompt;

        List<ChatMessage> messages =
        [
            new SystemChatMessage(populatedSystemPrompt),
            new UserChatMessage(userQuestion)
        ];

        var completion = await _chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        var rawText = completion.Value.Content[0].Text;

        return LlmPromptHelper.ParseResponse(rawText);
    }
}
