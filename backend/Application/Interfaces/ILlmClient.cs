namespace NeverMissLead.Application.Interfaces;

/// <summary>
/// Provider-agnostic contract for LLM text generation.
///
/// The active implementation is selected at runtime from
/// <c>AiProviders:LlmProvider</c> in configuration
/// (<c>none</c> | <c>ollama</c> | <c>openai</c> | <c>anthropic</c> | <c>gemini</c>).
/// Swapping providers requires only a config change — zero code changes, zero rebuild.
///
/// Default in local/demo mode: <c>NullLlmClient</c> (returns the top retrieved chunk
/// verbatim with a "based on your FAQ" wrapper — $0 cost, fully offline).
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Generates a grounded answer from the provided context chunks.
    /// </summary>
    /// <param name="systemPrompt">
    /// The system instruction that constrains the model to answer only from context
    /// and to abstain (return <c>null</c> / trigger handoff) when the context is insufficient.
    /// </param>
    /// <param name="userQuestion">The visitor's raw question.</param>
    /// <param name="contextChunks">
    /// The top-k KB chunks retrieved by the RAG service for this question.
    /// Must not be empty; callers should short-circuit to handoff before calling
    /// this method if retrieval returned nothing.
    /// </param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <returns>
    /// An <see cref="LlmResponse"/> containing the generated answer text and the
    /// chunk IDs that were actually cited, or <c>null</c> if the model determined
    /// it cannot answer from the provided context (triggers <c>needs_human</c>).
    /// </returns>
    Task<LlmResponse?> GenerateAsync(
        string systemPrompt,
        string userQuestion,
        IReadOnlyList<RetrievedChunk> contextChunks,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// The structured output of a single LLM generation call.
/// </summary>
/// <param name="AnswerText">
/// The generated answer to display to the visitor.
/// Never null when returned (a null <see cref="LlmResponse"/> itself signals abstention).
/// </param>
/// <param name="CitedChunkIds">
/// IDs of the <c>kb_chunks</c> rows whose text was used to produce the answer.
/// An empty list is a signal to flag <c>needs_human = true</c> on the conversation.
/// </param>
public sealed record LlmResponse(string AnswerText, IReadOnlyList<Guid> CitedChunkIds);
