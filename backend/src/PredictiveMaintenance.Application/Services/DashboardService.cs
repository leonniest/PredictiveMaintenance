using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.Application.Auth;
using PredictiveMaintenance.Application.Dashboard;
using PredictiveMaintenance.Application.Interfaces;
using PredictiveMaintenance.Domain.Entities;
using PredictiveMaintenance.Domain.Enums;

namespace PredictiveMaintenance.Application.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(UserContext user, CancellationToken cancellationToken);
    Task<IReadOnlyList<CompanyDto>> GetCompaniesAsync(UserContext user, CancellationToken cancellationToken);
    Task<CompanyDto?> GetCompanyAsync(UserContext user, Guid companyId, CancellationToken cancellationToken);
    Task<MachineDto?> GetMachineAsync(UserContext user, Guid machineId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertDto>> GetAlertsAsync(UserContext user, CancellationToken cancellationToken);
    Task<IReadOnlyList<PartLifetimeDto>> GetPartLifetimesAsync(UserContext user, CancellationToken cancellationToken);
    Task<IReadOnlyList<FailureTrendDto>> GetFailureTrendsAsync(UserContext user, CancellationToken cancellationToken);
}

public sealed class DashboardService(
    IPredictiveMaintenanceDbContext db,
    IPredictionService predictionService) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(UserContext user, CancellationToken cancellationToken)
    {
        var companyIds = await VisibleCompanyIds(user).ToListAsync(cancellationToken);
        var alerts = await AlertQuery(user).ToListAsync(cancellationToken);
        var openAlerts = alerts.Where(a => a.Status is AlertStatus.Open or AlertStatus.Notified).ToList();
        var companies = await CompanyQuery(user).OrderBy(c => c.Name).ToListAsync(cancellationToken);
        var fleetHealth = new Dictionary<Guid, int>();
        foreach (var company in companies)
        {
            fleetHealth[company.Id] = await GetCompanyFleetHealthAsync(company, cancellationToken);
        }

        var risks = new List<ResellerRiskDto>();
        foreach (var group in openAlerts.GroupBy(a => a.MachinePart.ControllerUnit.Machine.Company))
        {
            risks.Add(new ResellerRiskDto(
                group.Key.Id,
                group.Key.Name,
                group.Count(),
                group.Count(a => a.Severity == AlertSeverity.Critical),
                group.Min(a => a.PredictedDueDate),
                (int)group.Average(a => a.HealthScore),
                fleetHealth.GetValueOrDefault(group.Key.Id, 100)));
        }

        risks = risks
            .OrderBy(r => r.EarliestDueDate)
            .ThenBy(r => r.FleetHealthScore)
            .Take(6)
            .ToList();

        return new DashboardSummaryDto(
            companyIds.Count,
            await db.Machines.CountAsync(m => companyIds.Contains(m.CompanyId), cancellationToken),
            await db.MachineParts.CountAsync(p => companyIds.Contains(p.ControllerUnit.Machine.CompanyId), cancellationToken),
            openAlerts.Count,
            openAlerts.Count(a => a.Severity == AlertSeverity.Critical),
            risks,
            openAlerts.OrderBy(a => a.PredictedDueDate).Take(8).Select(ToAlertDto).ToList());
    }

    public async Task<IReadOnlyList<CompanyDto>> GetCompaniesAsync(UserContext user, CancellationToken cancellationToken)
    {
        var companies = await CompanyQuery(user).OrderBy(c => c.Name).ToListAsync(cancellationToken);
        var results = new List<CompanyDto>();
        foreach (var company in companies)
        {
            results.Add(await ToCompanyDto(company, cancellationToken));
        }

        return results;
    }

    public async Task<CompanyDto?> GetCompanyAsync(UserContext user, Guid companyId, CancellationToken cancellationToken)
    {
        var company = await CompanyQuery(user).SingleOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        return company is null ? null : await ToCompanyDto(company, cancellationToken);
    }

    public async Task<MachineDto?> GetMachineAsync(UserContext user, Guid machineId, CancellationToken cancellationToken)
    {
        var machine = await MachineQuery(user).SingleOrDefaultAsync(m => m.Id == machineId, cancellationToken);
        return machine is null ? null : await ToMachineDto(machine, cancellationToken);
    }

    public async Task<IReadOnlyList<AlertDto>> GetAlertsAsync(UserContext user, CancellationToken cancellationToken)
    {
        var alerts = await AlertQuery(user).OrderBy(a => a.PredictedDueDate).ToListAsync(cancellationToken);
        return alerts.Select(ToAlertDto).ToList();
    }

    public async Task<IReadOnlyList<PartLifetimeDto>> GetPartLifetimesAsync(UserContext user, CancellationToken cancellationToken)
    {
        var companyIds = await VisibleCompanyIds(user).ToListAsync(cancellationToken);
        var replacements = await db.TechnicianRecords
            .Include(r => r.MachinePart)
            .ThenInclude(p => p.ControllerUnit)
            .ThenInclude(c => c.Machine)
            .Where(r => r.WorkType == WorkType.Replacement && companyIds.Contains(r.MachinePart.ControllerUnit.Machine.CompanyId))
            .ToListAsync(cancellationToken);

        return replacements
            .GroupBy(r => r.MachinePart.Type)
            .Select(g => new PartLifetimeDto(
                g.Key,
                g.Count(),
                (int)g.Average(r => r.MovementCountAtService),
                (int)g.Average(r => r.DaysSinceInstallAtService),
                decimal.Round(g.Average(r => r.AverageTemperatureAtServiceC), 1),
                g.OrderByDescending(r => r.DaysSinceInstallAtService).First().MachinePart.Name,
                g.GroupBy(r => new
                    {
                        r.MachinePart.Manufacturer,
                        r.MachinePart.Model,
                        r.MachinePart.Name
                    })
                    .Select(modelGroup => new PartModelLifetimeDto(
                        modelGroup.Key.Manufacturer,
                        modelGroup.Key.Model,
                        modelGroup.Key.Name,
                        modelGroup.Count(),
                        (int)modelGroup.Average(r => r.MovementCountAtService),
                        (int)modelGroup.Average(r => r.DaysSinceInstallAtService),
                        decimal.Round(modelGroup.Average(r => r.AverageTemperatureAtServiceC), 1),
                        modelGroup.Max(r => r.DaysSinceInstallAtService),
                        modelGroup.Min(r => r.DaysSinceInstallAtService)))
                    .OrderByDescending(m => m.ReplacementSamples)
                    .ThenByDescending(m => m.AverageDaysToReplacement)
                    .ToList()))
            .OrderByDescending(r => r.AverageDaysToReplacement)
            .ToList();
    }

    public async Task<IReadOnlyList<FailureTrendDto>> GetFailureTrendsAsync(UserContext user, CancellationToken cancellationToken)
    {
        var companyIds = await VisibleCompanyIds(user).ToListAsync(cancellationToken);
        var records = await db.TechnicianRecords
            .Include(r => r.MachinePart)
            .ThenInclude(p => p.ControllerUnit)
            .ThenInclude(c => c.Machine)
            .Where(r => companyIds.Contains(r.MachinePart.ControllerUnit.Machine.CompanyId))
            .ToListAsync(cancellationToken);

        var start = new DateOnly(2025, 7, 1);
        var grouped = records.GroupBy(r => new DateOnly(r.PerformedOn.Year, r.PerformedOn.Month, 1))
            .ToDictionary(g => g.Key, g => g.ToList());

        return Enumerable.Range(0, 12)
            .Select(offset => start.AddMonths(offset))
            .Select(month =>
            {
                grouped.TryGetValue(month, out var monthRecords);
                monthRecords ??= [];
                return new FailureTrendDto(
                    $"{month.Year}-{month.Month:00}",
                    monthRecords.Count(r => r.WorkType == WorkType.Inspection),
                    monthRecords.Count(r => r.WorkType == WorkType.Repair),
                    monthRecords.Count(r => r.WorkType == WorkType.Replacement),
                    monthRecords.Count(r => r.WorkType == WorkType.FailureReport),
                    monthRecords.Count);
            })
            .ToList();
    }

    private IQueryable<Guid> VisibleCompanyIds(UserContext user)
        => user.Role == UserRole.Admin
            ? db.Companies.Select(c => c.Id)
            : db.Companies.Where(c => c.Id == user.CompanyId).Select(c => c.Id);

    private IQueryable<Company> CompanyQuery(UserContext user)
        => db.Companies
            .Include(c => c.Machines)
            .ThenInclude(m => m.Controller)
            .ThenInclude(cu => cu.Parts)
            .ThenInclude(p => p.Telemetry)
            .Include(c => c.Machines)
            .ThenInclude(m => m.Controller)
            .ThenInclude(cu => cu.Parts)
            .ThenInclude(p => p.Alerts)
            .Where(c => user.Role == UserRole.Admin || c.Id == user.CompanyId);

    private IQueryable<Machine> MachineQuery(UserContext user)
        => db.Machines
            .Include(m => m.Company)
            .Include(m => m.Controller)
            .ThenInclude(c => c.Parts)
            .ThenInclude(p => p.Telemetry)
            .Include(m => m.Controller)
            .ThenInclude(c => c.Parts)
            .ThenInclude(p => p.Alerts)
            .Where(m => user.Role == UserRole.Admin || m.CompanyId == user.CompanyId);

    private IQueryable<MaintenanceAlert> AlertQuery(UserContext user)
        => db.MaintenanceAlerts
            .Include(a => a.MachinePart)
            .ThenInclude(p => p.ControllerUnit)
            .ThenInclude(c => c.Machine)
            .ThenInclude(m => m.Company)
            .Where(a => user.Role == UserRole.Admin || a.MachinePart.ControllerUnit.Machine.CompanyId == user.CompanyId);

    private async Task<CompanyDto> ToCompanyDto(Company company, CancellationToken cancellationToken)
    {
        var machines = new List<MachineDto>();
        foreach (var machine in company.Machines.OrderBy(m => m.Name))
        {
            machines.Add(await ToMachineDto(machine, cancellationToken));
        }

        return new CompanyDto(
            company.Id,
            company.Name,
            company.ContactName,
            company.ContactEmail,
            company.Industry,
            company.Machines.Count,
            company.Machines.SelectMany(m => m.Controller.Parts).SelectMany(p => p.Alerts).Count(a => a.Status is AlertStatus.Open or AlertStatus.Notified),
            machines.Count == 0 ? 100 : (int)Math.Round(machines.Average(m => m.HealthScore)),
            machines);
    }

    private async Task<MachineDto> ToMachineDto(Machine machine, CancellationToken cancellationToken)
    {
        var parts = new List<PartDto>();
        foreach (var part in machine.Controller.Parts.OrderBy(p => p.Type).ThenBy(p => p.Name))
        {
            parts.Add(await ToPartDto(part, cancellationToken));
        }

        return new MachineDto(
            machine.Id,
            machine.CompanyId,
            machine.Company?.Name ?? string.Empty,
            machine.Name,
            machine.MachineType,
            machine.Location,
            machine.InstalledOn,
            new ControllerDto(
                machine.Controller.Id,
                machine.Controller.Kind,
                machine.Controller.Manufacturer,
                machine.Controller.Model,
                machine.Controller.SerialNumber,
                machine.Controller.FirmwareVersion,
                parts),
            machine.Controller.Parts.SelectMany(p => p.Alerts).Count(a => a.Status is AlertStatus.Open or AlertStatus.Notified),
            parts.Count == 0 ? 100 : (int)Math.Round(parts.Average(p => p.Prediction.HealthScore)));
    }

    private async Task<PartDto> ToPartDto(MachinePart part, CancellationToken cancellationToken)
    {
        var prediction = await predictionService.PredictAsync(part, cancellationToken);
        var telemetry = part.Telemetry.ToList();

        return new PartDto(
            part.Id,
            part.Name,
            part.Type,
            part.Manufacturer,
            part.Model,
            part.InstalledOn,
            part.LastReplacedOn,
            part.ReplacementCost,
            telemetry.Sum(t => t.MovementCount),
            telemetry.Sum(t => t.RotationCount),
            telemetry.Count == 0 ? 0 : decimal.Round(telemetry.Average(t => t.AverageTemperatureC), 1),
            prediction);
    }

    private async Task<int> GetCompanyFleetHealthAsync(Company company, CancellationToken cancellationToken)
    {
        var healthScores = new List<int>();
        foreach (var part in company.Machines.SelectMany(m => m.Controller.Parts))
        {
            var prediction = await predictionService.PredictAsync(part, cancellationToken);
            healthScores.Add(prediction.HealthScore);
        }

        return healthScores.Count == 0 ? 100 : (int)Math.Round(healthScores.Average());
    }

    private static AlertDto ToAlertDto(MaintenanceAlert alert)
        => new(
            alert.Id,
            alert.MachinePart.ControllerUnit.Machine.CompanyId,
            alert.MachinePart.ControllerUnit.Machine.Company.Name,
            alert.MachinePart.ControllerUnit.MachineId,
            alert.MachinePart.ControllerUnit.Machine.Name,
            alert.MachinePartId,
            alert.MachinePart.Name,
            alert.MachinePart.Type,
            alert.Severity,
            alert.Status,
            alert.CreatedAt,
            alert.PredictedDueDate,
            alert.HealthScore,
            alert.Message,
            alert.NotificationChannel,
            alert.NotifiedAt);
}
