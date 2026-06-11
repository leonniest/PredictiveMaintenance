using PredictiveMaintenance.Domain.Common;
using PredictiveMaintenance.Domain.Enums;

namespace PredictiveMaintenance.Domain.Entities;

public sealed class MachinePart : Entity
{
    public Guid ControllerUnitId { get; set; }
    public ControllerUnit ControllerUnit { get; set; } = default!;
    public string Name { get; set; } = string.Empty;
    public PartType Type { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateOnly InstalledOn { get; set; }
    public DateOnly? LastReplacedOn { get; set; }
    public decimal ReplacementCost { get; set; }
    public ICollection<TelemetryDailySummary> Telemetry { get; set; } = new List<TelemetryDailySummary>();
    public ICollection<TechnicianRecord> TechnicianRecords { get; set; } = new List<TechnicianRecord>();
    public ICollection<MaintenanceAlert> Alerts { get; set; } = new List<MaintenanceAlert>();
}
