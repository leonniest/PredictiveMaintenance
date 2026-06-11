using PredictiveMaintenance.Domain.Common;
using PredictiveMaintenance.Domain.Enums;

namespace PredictiveMaintenance.Domain.Entities;

public sealed class MaintenanceAlert : Entity
{
    public Guid MachinePartId { get; set; }
    public MachinePart MachinePart { get; set; } = default!;
    public AlertSeverity Severity { get; set; }
    public AlertStatus Status { get; set; } = AlertStatus.Open;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateOnly PredictedDueDate { get; set; }
    public int HealthScore { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ReasonCodes { get; set; } = string.Empty;
    public string? NotificationChannel { get; set; }
    public DateTimeOffset? NotifiedAt { get; set; }
}
