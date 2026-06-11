using PredictiveMaintenance.Domain.Common;
using PredictiveMaintenance.Domain.Enums;

namespace PredictiveMaintenance.Domain.Entities;

public sealed class ControllerUnit : Entity
{
    public Guid MachineId { get; set; }
    public Machine Machine { get; set; } = default!;
    public ControllerKind Kind { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public ICollection<MachinePart> Parts { get; set; } = new List<MachinePart>();
}
