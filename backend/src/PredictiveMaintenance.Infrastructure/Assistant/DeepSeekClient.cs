using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PredictiveMaintenance.Application.Interfaces;

namespace PredictiveMaintenance.Infrastructure.Assistant;

public sealed class DeepSeekClient(HttpClient httpClient, IOptions<DeepSeekOptions> options) : IDeepSeekClient
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.ApiKey);

    public async Task<string> GetAssistantReplyAsync(IReadOnlyList<AssistantMessage> messages, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!IsConfigured)
        {
            return "DeepSeek is not configured. Set DeepSeek__ApiKey on the API container to enable live LLM answers.";
        }

        httpClient.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

        var response = await httpClient.PostAsJsonAsync(
            "chat/completions",
            new ChatCompletionRequest(
                settings.Model,
                messages.Select(m => new ChatCompletionMessage(m.Role, m.Content)).ToList(),
                0.2m),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: cancellationToken);
        return payload?.Choices.FirstOrDefault()?.Message.Content?.Trim()
            ?? "I could not generate a response from the configured model.";
    }

    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatCompletionMessage> Messages,
        [property: JsonPropertyName("temperature")] decimal Temperature);

    private sealed record ChatCompletionMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatCompletionResponse(
        [property: JsonPropertyName("choices")] IReadOnlyList<ChatCompletionChoice> Choices);

    private sealed record ChatCompletionChoice(
        [property: JsonPropertyName("message")] ChatCompletionMessage Message);
}
