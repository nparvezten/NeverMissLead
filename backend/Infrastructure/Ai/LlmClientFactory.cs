using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeverMissLead.Application.Interfaces;

namespace NeverMissLead.Infrastructure.Ai;

/// <summary>
/// Factory for selecting and instantiating the active <see cref="ILlmClient"/> at startup.
/// Selected via <c>AiProviders:LlmProvider</c> in configuration or the <c>LLM_PROVIDER</c> env var.
/// </summary>
public static class LlmClientFactory
{
    public static ILlmClient Create(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var provider = (configuration["AiProviders:LlmProvider"]
            ?? configuration["LLM_PROVIDER"]
            ?? "none").Trim().ToLowerInvariant();

        return provider switch
        {
            "none" or "" => new NullLlmClient(),
            "ollama" => serviceProvider.GetRequiredService<OllamaLlmClient>(),
            "openai" => new OpenAiLlmClient(configuration),
            "anthropic" => serviceProvider.GetRequiredService<AnthropicLlmClient>(),
            "gemini" => serviceProvider.GetRequiredService<GeminiLlmClient>(),
            _ => throw new InvalidOperationException(
                $"Unrecognized LLM provider '{provider}'. Valid options: 'none', 'ollama', 'openai', 'anthropic', 'gemini'.")
        };
    }
}
