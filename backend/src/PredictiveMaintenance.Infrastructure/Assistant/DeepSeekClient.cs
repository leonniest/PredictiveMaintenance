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
            return "DeepSeek is not configured. Set DEEPSEEK_API_KEY in the root .env file and restart the API container to enable live LLM answers.";
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(settings.BaseUrl.TrimEnd('/') + "/chat/completions"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Content = JsonContent.Create(new ChatCompletionRequest(
            settings.Model,
            messages.Select(m => new ChatCompletionMessage(m.Role, m.Content)).ToList(),
            0.2m));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ChatCompletionErrorEnvelope>(cancellationToken: cancellationToken);
            var reason = error?.Error?.Message ?? response.ReasonPhrase ?? "unknown error";
            return $"The DeepSeek API rejected the request ({(int)response.StatusCode}): {reason}. Check the API key, model name and account balance at platform.deepseek.com.";
        }

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

    private sealed record ChatCompletionErrorEnvelope(
        [property: JsonPropertyName("error")] ChatCompletionError? Error);

    private sealed record ChatCompletionError(
        [property: JsonPropertyName("message")] string? Message);
}
