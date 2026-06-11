using PredictiveMaintenance.Domain.Common;

namespace PredictiveMaintenance.Domain.Entities;

public sealed class ResellerSupportSettings : Entity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = default!;
    public string SupportDepartmentEmail { get; set; } = string.Empty;
    public string TechnicianDispatchEmail { get; set; } = string.Empty;
    public string EscalationPhone { get; set; } = string.Empty;
    public int DefaultSlaHours { get; set; } = 24;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
