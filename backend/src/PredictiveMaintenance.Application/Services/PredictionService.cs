using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.Application.Dashboard;
using PredictiveMaintenance.Application.Interfaces;
using PredictiveMaintenance.Domain.Entities;
using PredictiveMaintenance.Domain.Enums;

namespace PredictiveMaintenance.Application.Services;

public interface IPredictionService
{
    Task<PredictionDto> PredictAsync(MachinePart part, CancellationToken cancellationToken);
}

public sealed class PredictionService(IPredictiveMaintenanceDbContext db) : IPredictionService
{
    private static readonly DateOnly Today = new(2026, 6, 11);
    private readonly Dictionary<PartType, List<TechnicianRecord>> replacementSampleCache = [];

    public async Task<PredictionDto> PredictAsync(MachinePart part, CancellationToken cancellationToken)
    {
        var samples = await GetReplacementSamplesAsync(part.Type, cancellationToken);

        var telemetry = part.Telemetry.Any()
            ? part.Telemetry
            : await db.TelemetryDailySummaries.Where(t => t.MachinePartId == part.Id).ToListAsync(cancellationToken);

        var totalMovements = telemetry.Sum(t => t.MovementCount);
        var averageTemperature = telemetry.Any() ? telemetry.Average(t => t.AverageTemperatureC) : 0m;
        var installedOrReplaced = part.LastReplacedOn ?? part.InstalledOn;
        var ageDays = Math.Max(1, Today.DayNumber - installedOrReplaced.DayNumber);

        var baselineMovements = samples.Count > 0 ? (decimal)samples.Average(s => s.MovementCountAtService) : DefaultMovementBaseline(part.Type);
        var baselineDays = samples.Count > 0 ? (decimal)samples.Average(s => s.DaysSinceInstallAtService) : DefaultDayBaseline(part.Type);
        var baselineTemperature = samples.Count > 0 ? samples.Average(s => s.AverageTemperatureAtServiceC) : DefaultTemperatureBaseline(part.Type);

        var movementRatio = SafeRatio(totalMovements, baselineMovements);
        var ageRatio = SafeRatio(ageDays, baselineDays);
        var temperatureRatio = baselineTemperature <= 0 ? 0 : Math.Max(0, (averageTemperature - baselineTemperature + 8m) / 16m);
        var lifecycleRatio = (Math.Min(movementRatio, 1.35m) * 0.50m)
            + (Math.Min(ageRatio, 1.35m) * 0.30m)
            + (Math.Min(temperatureRatio, 1.35m) * 0.20m);
        var riskRatio = Math.Clamp((lifecycleRatio - 0.35m) / 0.90m, 0m, 1.15m);

        var healthScore = Math.Clamp(100 - (int)Math.Round(riskRatio * 88m), 0, 100);
        var severity = healthScore <= 35 ? AlertSeverity.Critical : healthScore <= 65 ? AlertSeverity.Warning : AlertSeverity.Info;
        var dueRatio = Math.Max(0.05m, lifecycleRatio);
        var projectedRemainingDays = (int)Math.Round(ageDays * ((1 / dueRatio) - 1));
        var predictedDueDate = Today.AddDays(projectedRemainingDays);
        var confidence = Math.Clamp(samples.Count / 8m, 0.25m, 0.95m);

        var reasons = new List<string>();
        if (movementRatio >= 1m)
        {
            reasons.Add($"Usage has exceeded the historical replacement movement volume ({totalMovements:N0}/{baselineMovements:N0} cycles) by {movementRatio - 1m:P0}.");
        }
        else if (movementRatio >= 0.75m)
        {
            reasons.Add($"Usage is at {movementRatio:P0} of historical replacement movement volume ({totalMovements:N0}/{baselineMovements:N0} cycles).");
        }

        if (ageRatio >= 1m)
        {
            reasons.Add($"Installed age has passed the historical replacement age ({ageDays:N0}/{baselineDays:N0} days) by {ageRatio - 1m:P0}.");
        }
        else if (ageRatio >= 0.75m)
        {
            reasons.Add($"Installed age is at {ageRatio:P0} of historical replacement age ({ageDays:N0}/{baselineDays:N0} days).");
        }

        if (temperatureRatio >= 0.75m)
        {
            reasons.Add($"Temperature profile is trending near replacement history for {part.Type} parts.");
        }

        if (reasons.Count == 0)
        {
            reasons.Add("Part is inside expected historical operating range.");
        }

        return new PredictionDto(
            healthScore,
            predictedDueDate,
            severity,
            decimal.Round(confidence, 2),
            reasons,
            decimal.Round(movementRatio, 2),
            decimal.Round(ageRatio, 2),
            decimal.Round(temperatureRatio, 2),
            (int)Math.Round(baselineMovements),
            (int)Math.Round(baselineDays),
            decimal.Round(baselineTemperature, 1));
    }

    private async Task<List<TechnicianRecord>> GetReplacementSamplesAsync(PartType type, CancellationToken cancellationToken)
    {
        if (replacementSampleCache.TryGetValue(type, out var samples))
        {
            return samples;
        }

        samples = await db.TechnicianRecords
            .Include(r => r.MachinePart)
            .Where(r => r.WorkType == WorkType.Replacement && r.MachinePart.Type == type)
            .ToListAsync(cancellationToken);
        replacementSampleCache[type] = samples;
        return samples;
    }

    private static decimal SafeRatio(decimal current, decimal baseline)
        => baseline <= 0 ? 0 : current / baseline;

    private static decimal DefaultMovementBaseline(PartType type)
        => type switch
        {
            PartType.Motor => 1_200_000,
            PartType.Belt => 650_000,
            PartType.Relay => 900_000,
            PartType.Sensor => 1_500_000,
            _ => 1_000_000
        };

    private static decimal DefaultDayBaseline(PartType type)
        => type switch
        {
            PartType.Motor => 780,
            PartType.Belt => 520,
            PartType.Relay => 680,
            PartType.Sensor => 900,
            _ => 700
        };

    private static decimal DefaultTemperatureBaseline(PartType type)
        => type switch
        {
            PartType.Motor => 67,
            PartType.Belt => 48,
            PartType.Relay => 54,
            PartType.Sensor => 43,
            _ => 50
        };
}
