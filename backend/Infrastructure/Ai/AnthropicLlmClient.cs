using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using NeverMissLead.Application.Interfaces;

namespace NeverMissLead.Infrastructure.Ai;

/// <summary>
/// Client-ready LLM provider for Anthropic Claude (e.g. claude-3-haiku, claude-3-5-sonnet).
/// </summary>
public sealed class AnthropicLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public AnthropicLlmClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var apiKey = configuration["AiProviders:Anthropic:ApiKey"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Anthropic API key is missing. Set 'AiProviders:Anthropic:ApiKey' in appsettings.Local.json or via the ANTHROPIC_API_KEY environment variable.");
        }

        _httpClient.BaseAddress = new Uri("https://api.anthropic.com/v1/");
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _model = configuration["AiProviders:Anthropic:Model"] ?? "claude-3-haiku-20240307";
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

        var requestBody = new AnthropicMessagesRequest
        {
            Model = _model,
            MaxTokens = 1024,
            System = populatedSystemPrompt,
            Messages =
            [
                new AnthropicMessage { Role = "user", Content = userQuestion }
            ]
        };

        var response = await _httpClient.PostAsJsonAsync("messages", requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnthropicMessagesResponse>(cancellationToken: cancellationToken);
        var rawText = result?.Content?.FirstOrDefault()?.Text;

        return LlmPromptHelper.ParseResponse(rawText);
    }

    private sealed class AnthropicMessagesRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 1024;

        [JsonPropertyName("system")]
        public string System { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<AnthropicMessage> Messages { get; set; } = [];
    }

    private sealed class AnthropicMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class AnthropicMessagesResponse
    {
        [JsonPropertyName("content")]
        public List<AnthropicContentBlock>? Content { get; set; }
    }

    private sealed class AnthropicContentBlock
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
