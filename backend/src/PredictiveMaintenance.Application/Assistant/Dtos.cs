namespace PredictiveMaintenance.Application.Assistant;

public sealed record AssistantSessionDto(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<AssistantChatMessageDto> Messages);

public sealed record AssistantChatMessageDto(
    Guid Id,
    string Role,
    string Content,
    DateTimeOffset CreatedAt);

public sealed record CreateAssistantSessionRequest(string? Title);

public sealed record SendAssistantMessageRequest(string Content);

public sealed record FaqPromptDto(string Label, string Prompt);
