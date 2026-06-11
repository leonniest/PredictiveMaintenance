using PredictiveMaintenance.Domain.Common;

namespace PredictiveMaintenance.Domain.Entities;

public sealed class TelemetryDailySummary : Entity
{
    public Guid MachinePartId { get; set; }
    public MachinePart MachinePart { get; set; } = default!;
    public DateOnly Date { get; set; }
    public int MovementCount { get; set; }
    public int RotationCount { get; set; }
    public decimal OperatingHours { get; set; }
    public decimal AverageTemperatureC { get; set; }
    public decimal MaxTemperatureC { get; set; }
}
