using System.Net.Http.Json;
using System.Text.Json;
using NeverMissLead.Application.Interfaces;

namespace NeverMissLead.Infrastructure.RagClient;

/// <summary>
/// HTTP client implementation of <see cref="IRagClient"/>.
/// Calls the Python FastAPI RAG microservice.
/// Configured via <c>RagService:BaseUrl</c> in appsettings.
/// </summary>
public sealed class RagHttpClient : IRagClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public RagHttpClient(HttpClient http) => _http = http;

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> EmbedAsync(
        Guid businessId,
        Guid documentId,
        string text,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            business_id = businessId,
            document_id = documentId,
            text
        };

        var response = await _http.PostAsJsonAsync("/embed", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<EmbedResponse>(JsonOptions, cancellationToken);

        return result?.ChunkIds ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RetrievedChunk>> QueryAsync(
        Guid businessId,
        string question,
        int topK = 4,
        CancellationToken cancellationToken = default)
    {
        var payload = new { business_id = businessId, question, top_k = topK };

        var response = await _http.PostAsJsonAsync("/query", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<QueryResponse>(JsonOptions, cancellationToken);

        return result?.Chunks
            .Select(c => new RetrievedChunk(c.ChunkId, c.ChunkText, c.Score))
            .ToList()
            ?? [];
    }

    // ── Private DTOs matching the Python response schema ──────────────────────

    private sealed record EmbedResponse(IReadOnlyList<Guid> ChunkIds, int ChunksCreated);

    private sealed record QueryResponse(IReadOnlyList<ChunkDto> Chunks);

    private sealed record ChunkDto(Guid ChunkId, string ChunkText, double Score);
}

