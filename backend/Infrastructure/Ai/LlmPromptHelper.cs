using System.Text;
using System.Text.RegularExpressions;
using NeverMissLead.Application.Interfaces;

namespace NeverMissLead.Infrastructure.Ai;

/// <summary>
/// Shared prompt templates and output parsing logic for all LLM providers.
/// </summary>
public static partial class LlmPromptHelper
{
    public const string SystemPromptTemplate =
@"You are a helpful assistant for a service business.
Answer ONLY using the context provided below.
At the end of your answer, list the IDs of the context chunks you used, in this format:
CITED_CHUNKS: <chunk_id_1>,<chunk_id_2>

If the context does not contain the answer, reply EXACTLY:
""I'm not sure about that — let me connect you with the owner.""
Do not make up information not present in the context.

Context:
{0}";

    public const string AbstainPhrase = "I'm not sure about that — let me connect you with the owner.";

    /// <summary>
    /// Builds the system prompt populated with context chunk IDs and texts.
    /// </summary>
    public static string BuildSystemPrompt(IReadOnlyList<RetrievedChunk> contextChunks)
    {
        var sb = new StringBuilder();
        foreach (var chunk in contextChunks)
        {
            sb.AppendLine($"{chunk.ChunkId}: {chunk.ChunkText}");
        }

        return string.Format(SystemPromptTemplate, sb.ToString().TrimEnd());
    }

    /// <summary>
    /// Combines system prompt, context chunks, and user question into a single text prompt
    /// for completion models (e.g. Ollama).
    /// </summary>
    public static string BuildFullPrompt(string systemPrompt, string userQuestion, IReadOnlyList<RetrievedChunk> contextChunks)
    {
        var populatedSystemPrompt = systemPrompt.Contains("{0}")
            ? BuildSystemPrompt(contextChunks)
            : systemPrompt;

        return $"{populatedSystemPrompt}\n\nUser Question:\n{userQuestion}\n\nAnswer:";
    }

    /// <summary>
    /// Parses the raw output of an LLM call. Detects abstentions and extracts cited chunk IDs.
    /// Returns null if the model abstains or if zero valid chunk IDs were cited.
    /// </summary>
    public static LlmResponse? ParseResponse(string? rawResponse)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
            return null;

        var text = rawResponse.Trim();

        // 1. Check for exact or substring abstention phrases
        if (text.Contains(AbstainPhrase, StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("I'm not sure", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("let me connect you with the owner", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // 2. Parse CITED_CHUNKS: <guid1>,<guid2>
        var citedChunkIds = new List<Guid>();
        var match = CitedChunksRegex().Match(text);

        if (match.Success)
        {
            var idsString = match.Groups[1].Value;
            var parts = idsString.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (Guid.TryParse(part.Trim(), out var id))
                {
                    citedChunkIds.Add(id);
                }
            }

            // Remove the CITED_CHUNKS: ... line from the visible answer text
            text = CitedChunksRegex().Replace(text, string.Empty).Trim();
        }

        // 3. Abstention rule: if no chunk IDs were cited, return null
        if (citedChunkIds.Count == 0)
            return null;

        return new LlmResponse(text, citedChunkIds);
    }

    [GeneratedRegex(@"CITED_CHUNKS:\s*([a-fA-F0-9,\s\-;]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex CitedChunksRegex();
}
