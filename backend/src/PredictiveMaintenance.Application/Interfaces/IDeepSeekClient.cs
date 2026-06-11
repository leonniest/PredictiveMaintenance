namespace PredictiveMaintenance.Application.Interfaces;

public interface IDeepSeekClient
{
    bool IsConfigured { get; }
    Task<string> GetAssistantReplyAsync(IReadOnlyList<AssistantMessage> messages, CancellationToken cancellationToken);
}

public sealed record AssistantMessage(string Role, string Content);
