using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.Application.Interfaces;
using PredictiveMaintenance.Domain.Entities;

namespace PredictiveMaintenance.Infrastructure.Persistence;

public sealed class PredictiveMaintenanceDbContext(DbContextOptions<PredictiveMaintenanceDbContext> options)
    : DbContext(options), IPredictiveMaintenanceDbContext
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<ControllerUnit> ControllerUnits => Set<ControllerUnit>();
    public DbSet<MachinePart> MachineParts => Set<MachinePart>();
    public DbSet<TelemetryDailySummary> TelemetryDailySummaries => Set<TelemetryDailySummary>();
    public DbSet<TechnicianRecord> TechnicianRecords => Set<TechnicianRecord>();
    public DbSet<MaintenanceAlert> MaintenanceAlerts => Set<MaintenanceAlert>();
    public DbSet<ResellerSupportSettings> ResellerSupportSettings => Set<ResellerSupportSettings>();
    public DbSet<AssistantChatSession> AssistantChatSessions => Set<AssistantChatSession>();
    public DbSet<AssistantChatMessage> AssistantChatMessages => Set<AssistantChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(180);
            entity.Property(e => e.ContactName).HasMaxLength(120);
            entity.Property(e => e.ContactEmail).HasMaxLength(220);
            entity.Property(e => e.Industry).HasMaxLength(120);
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.Property(e => e.DisplayName).HasMaxLength(120);
            entity.Property(e => e.Email).HasMaxLength(220);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Machine>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(180);
            entity.Property(e => e.MachineType).HasMaxLength(80);
            entity.Property(e => e.Location).HasMaxLength(180);
            entity.HasOne(e => e.Controller)
                .WithOne(e => e.Machine)
                .HasForeignKey<ControllerUnit>(e => e.MachineId);
        });

        modelBuilder.Entity<ControllerUnit>(entity =>
        {
            entity.Property(e => e.Manufacturer).HasMaxLength(100);
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.Property(e => e.FirmwareVersion).HasMaxLength(50);
        });

        modelBuilder.Entity<MachinePart>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(180);
            entity.Property(e => e.Manufacturer).HasMaxLength(100);
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.Property(e => e.ReplacementCost).HasPrecision(12, 2);
        });

        modelBuilder.Entity<TelemetryDailySummary>(entity =>
        {
            entity.HasIndex(e => new { e.MachinePartId, e.Date }).IsUnique();
            entity.Property(e => e.OperatingHours).HasPrecision(8, 2);
            entity.Property(e => e.AverageTemperatureC).HasPrecision(6, 2);
            entity.Property(e => e.MaxTemperatureC).HasPrecision(6, 2);
        });

        modelBuilder.Entity<TechnicianRecord>(entity =>
        {
            entity.Property(e => e.TechnicianName).HasMaxLength(120);
            entity.Property(e => e.Notes).HasMaxLength(600);
            entity.Property(e => e.LaborHours).HasPrecision(8, 2);
            entity.Property(e => e.PartCost).HasPrecision(12, 2);
            entity.Property(e => e.AverageTemperatureAtServiceC).HasPrecision(6, 2);
        });

        modelBuilder.Entity<MaintenanceAlert>(entity =>
        {
            entity.Property(e => e.Message).HasMaxLength(400);
            entity.Property(e => e.ReasonCodes).HasMaxLength(1200);
            entity.Property(e => e.NotificationChannel).HasMaxLength(40);
        });

        modelBuilder.Entity<ResellerSupportSettings>(entity =>
        {
            entity.Property(e => e.SupportDepartmentEmail).HasMaxLength(220);
            entity.Property(e => e.TechnicianDispatchEmail).HasMaxLength(220);
            entity.Property(e => e.EscalationPhone).HasMaxLength(60);
            entity.HasIndex(e => e.CompanyId).IsUnique();
            entity.HasOne(e => e.Company)
                .WithOne(e => e.SupportSettings)
                .HasForeignKey<ResellerSupportSettings>(e => e.CompanyId);
        });

        modelBuilder.Entity<AssistantChatSession>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(140);
            entity.HasOne(e => e.AppUser)
                .WithMany(e => e.ChatSessions)
                .HasForeignKey(e => e.AppUserId);
        });

        modelBuilder.Entity<AssistantChatMessage>(entity =>
        {
            entity.Property(e => e.Role).HasMaxLength(20);
            entity.Property(e => e.Content).HasMaxLength(4000);
            entity.HasOne(e => e.AssistantChatSession)
                .WithMany(e => e.Messages)
                .HasForeignKey(e => e.AssistantChatSessionId);
        });
    }
}
