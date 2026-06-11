using PredictiveMaintenance.Domain.Common;

namespace PredictiveMaintenance.Domain.Entities;

public sealed class Machine : Entity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = default!;
    public string Name { get; set; } = string.Empty;
    public string MachineType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateOnly InstalledOn { get; set; }
    public ControllerUnit Controller { get; set; } = default!;
}
