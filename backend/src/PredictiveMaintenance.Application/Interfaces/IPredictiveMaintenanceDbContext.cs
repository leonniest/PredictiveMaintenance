using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.Domain.Entities;

namespace PredictiveMaintenance.Application.Interfaces;

public interface IPredictiveMaintenanceDbContext
{
    DbSet<AppUser> Users { get; }
    DbSet<Company> Companies { get; }
    DbSet<Machine> Machines { get; }
    DbSet<ControllerUnit> ControllerUnits { get; }
    DbSet<MachinePart> MachineParts { get; }
    DbSet<TelemetryDailySummary> TelemetryDailySummaries { get; }
    DbSet<TechnicianRecord> TechnicianRecords { get; }
    DbSet<MaintenanceAlert> MaintenanceAlerts { get; }
    DbSet<ResellerSupportSettings> ResellerSupportSettings { get; }
    DbSet<AssistantChatSession> AssistantChatSessions { get; }
    DbSet<AssistantChatMessage> AssistantChatMessages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
