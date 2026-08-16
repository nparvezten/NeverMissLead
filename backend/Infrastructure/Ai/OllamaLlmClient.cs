using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using NeverMissLead.Application.Interfaces;

namespace NeverMissLead.Infrastructure.Ai;

/// <summary>
/// Free, local LLM provider calling an Ollama instance on CPU/GPU.
/// </summary>
public sealed class OllamaLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public OllamaLlmClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var baseUrl = configuration["AiProviders:Ollama:BaseUrl"] ?? "http://localhost:11434";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _model = configuration["AiProviders:Ollama:Model"] ?? "mistral";
    }

    public async Task<LlmResponse?> GenerateAsync(
        string systemPrompt,
        string userQuestion,
        IReadOnlyList<RetrievedChunk> contextChunks,
        CancellationToken cancellationToken = default)
    {
        if (contextChunks == null || contextChunks.Count == 0)
            return null;

        var prompt = LlmPromptHelper.BuildFullPrompt(systemPrompt, userQuestion, contextChunks);

        var requestBody = new OllamaGenerateRequest
        {
            Model = _model,
            Prompt = prompt,
            Stream = false
        };

        var response = await _httpClient.PostAsJsonAsync("/api/generate", requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: cancellationToken);

        return LlmPromptHelper.ParseResponse(result?.Response);
    }

    private sealed class OllamaGenerateRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private sealed class OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }
}
