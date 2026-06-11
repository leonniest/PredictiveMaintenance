using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.Application.Interfaces;
using PredictiveMaintenance.Application.Services;
using PredictiveMaintenance.Domain.Entities;
using PredictiveMaintenance.Domain.Enums;

namespace PredictiveMaintenance.Tests;

public sealed class PredictionServiceTests
{
    [Fact]
    public async Task PredictAsync_MarksOverusedMotorAsCritical()
    {
        await using var db = TestContext.Create();
        var part = new MachinePart
        {
            Name = "High-cycle motor",
            Type = PartType.Motor,
            InstalledOn = new DateOnly(2024, 1, 1),
            LastReplacedOn = new DateOnly(2024, 1, 1)
        };

        part.Telemetry.Add(new TelemetryDailySummary
        {
            Date = new DateOnly(2026, 6, 1),
            MovementCount = 1_650_000,
            RotationCount = 2_250_000,
            OperatingHours = 1300,
            AverageTemperatureC = 80,
            MaxTemperatureC = 88
        });

        db.MachineParts.Add(part);
        db.TechnicianRecords.Add(new TechnicianRecord
        {
            MachinePart = part,
            WorkType = WorkType.Replacement,
            PerformedOn = new DateOnly(2024, 12, 1),
            TechnicianName = "Test Technician",
            Notes = "Historical replacement baseline.",
            LaborHours = 2,
            PartCost = 900,
            MovementCountAtService = 1_100_000,
            RotationCountAtService = 1_650_000,
            DaysSinceInstallAtService = 720,
            AverageTemperatureAtServiceC = 66
        });
        await db.SaveChangesAsync();

        var service = new PredictionService(db);
        var prediction = await service.PredictAsync(part, CancellationToken.None);

        Assert.Equal(AlertSeverity.Critical, prediction.Severity);
        Assert.True(prediction.HealthScore <= 35);
        Assert.Contains(prediction.ReasonCodes, reason => reason.Contains("Usage", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PredictAsync_KeepsLowUsageSensorInformational()
    {
        await using var db = TestContext.Create();
        var part = new MachinePart
        {
            Name = "Low-cycle sensor",
            Type = PartType.Sensor,
            InstalledOn = new DateOnly(2026, 1, 1)
        };

        part.Telemetry.Add(new TelemetryDailySummary
        {
            Date = new DateOnly(2026, 6, 1),
            MovementCount = 120_000,
            OperatingHours = 260,
            AverageTemperatureC = 35,
            MaxTemperatureC = 39
        });

        db.MachineParts.Add(part);
        await db.SaveChangesAsync();

        var service = new PredictionService(db);
        var prediction = await service.PredictAsync(part, CancellationToken.None);

        Assert.Equal(AlertSeverity.Info, prediction.Severity);
        Assert.True(prediction.HealthScore > 70);
    }

    private sealed class TestContext(DbContextOptions<TestContext> options)
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

        public static TestContext Create()
        {
            var options = new DbContextOptionsBuilder<TestContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new TestContext(options);
        }
    }
}
