using NeverMissLead.Application.Interfaces;

namespace NeverMissLead.Infrastructure.Ai;

/// <summary>
/// Default $0 LLM client used when <c>AiProviders:LlmProvider</c> is <c>"none"</c>.
/// Returns the top retrieved chunk verbatim with a "Based on our FAQ" wrapper and
/// records its chunk ID as cited. Makes zero external API calls.
/// </summary>
public sealed class NullLlmClient : ILlmClient
{
    public Task<LlmResponse?> GenerateAsync(
        string systemPrompt,
        string userQuestion,
        IReadOnlyList<RetrievedChunk> contextChunks,
        CancellationToken cancellationToken = default)
    {
        if (contextChunks == null || contextChunks.Count == 0)
            return Task.FromResult<LlmResponse?>(null);

        var topChunk = contextChunks[0];
        var answer = $"Based on our FAQ: {topChunk.ChunkText}";
        var response = new LlmResponse(answer, [topChunk.ChunkId]);

        return Task.FromResult<LlmResponse?>(response);
    }
}
