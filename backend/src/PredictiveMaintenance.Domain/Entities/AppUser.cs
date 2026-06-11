using PredictiveMaintenance.Domain.Common;
using PredictiveMaintenance.Domain.Enums;

namespace PredictiveMaintenance.Domain.Entities;

public sealed class AppUser : Entity
{
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public ICollection<AssistantChatSession> ChatSessions { get; set; } = new List<AssistantChatSession>();
}
