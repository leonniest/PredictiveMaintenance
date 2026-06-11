using PredictiveMaintenance.Domain.Common;

namespace PredictiveMaintenance.Domain.Entities;

public sealed class AssistantChatMessage : Entity
{
    public Guid AssistantChatSessionId { get; set; }
    public AssistantChatSession AssistantChatSession { get; set; } = default!;
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
