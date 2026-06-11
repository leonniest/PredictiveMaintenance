using PredictiveMaintenance.Domain.Common;

namespace PredictiveMaintenance.Domain.Entities;

public sealed class AssistantChatSession : Entity
{
    public Guid AppUserId { get; set; }
    public AppUser AppUser { get; set; } = default!;
    public string Title { get; set; } = "Maintenance assistant";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<AssistantChatMessage> Messages { get; set; } = new List<AssistantChatMessage>();
}
