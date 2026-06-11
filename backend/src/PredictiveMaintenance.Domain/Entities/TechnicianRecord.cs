using PredictiveMaintenance.Domain.Common;
using PredictiveMaintenance.Domain.Enums;

namespace PredictiveMaintenance.Domain.Entities;

public sealed class TechnicianRecord : Entity
{
    public Guid MachinePartId { get; set; }
    public MachinePart MachinePart { get; set; } = default!;
    public WorkType WorkType { get; set; }
    public DateOnly PerformedOn { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public decimal LaborHours { get; set; }
    public decimal PartCost { get; set; }
    public int MovementCountAtService { get; set; }
    public int RotationCountAtService { get; set; }
    public int DaysSinceInstallAtService { get; set; }
    public decimal AverageTemperatureAtServiceC { get; set; }
}
