using PredictiveMaintenance.Domain.Common;

namespace PredictiveMaintenance.Domain.Entities;

public sealed class Company : Entity
{
    public string Name { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public ICollection<Machine> Machines { get; set; } = new List<Machine>();
    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ResellerSupportSettings? SupportSettings { get; set; }
}
