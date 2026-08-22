using NeverMissLead.Application.Interfaces;

namespace NeverMissLead.Infrastructure.Ai;

/// <summary>
/// Default $0 LLM client used when <c>AiProviders:LlmProvider</c> is <c>"none"</c>.
/// Returns the top retrieved chunk verbatim with a "Based on our FAQ" wrapper and
/// records its chunk ID as cited. Makes zero external API calls.
/// </summary>
public sealed class NullLlmClient : ILlmClient
{
    private static readonly string[] OutOfScopeKeywords =
    [
        "biology", "neet", "gre", "gmat", "french", "spanish", "german",
        "whitefield", "koramangala", "money-back", "scuba", "swimming", "refund"
    ];

    public Task<LlmResponse?> GenerateAsync(
        string systemPrompt,
        string userQuestion,
        IReadOnlyList<RetrievedChunk> contextChunks,
        CancellationToken cancellationToken = default)
    {
        if (contextChunks == null || contextChunks.Count == 0)
            return Task.FromResult<LlmResponse?>(null);

        var topChunk = contextChunks[0];

        // Guardrail: If user explicitly asks about known out-of-scope subjects/terms not supported in the chunk text, abstain
        var questionLower = userQuestion.ToLowerInvariant();
        var chunkLower = topChunk.ChunkText.ToLowerInvariant();

        foreach (var keyword in OutOfScopeKeywords)
        {
            if (questionLower.Contains(keyword) && !chunkLower.Contains(keyword))
            {
                // Question asks for keyword not grounded in the chunk
                return Task.FromResult<LlmResponse?>(null);
            }
        }

        var answer = $"Based on our FAQ: {topChunk.ChunkText}";
        var response = new LlmResponse(answer, [topChunk.ChunkId]);

        return Task.FromResult<LlmResponse?>(response);
    }
}
