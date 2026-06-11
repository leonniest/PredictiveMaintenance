using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.Application.Interfaces;
using PredictiveMaintenance.Domain.Entities;
using PredictiveMaintenance.Domain.Enums;

namespace PredictiveMaintenance.Application.Services;

public interface IAlertService
{
    Task<int> EvaluateAsync(CancellationToken cancellationToken);
    Task<bool> AcknowledgeAsync(Guid alertId, CancellationToken cancellationToken);
    Task<bool> NotifyAsync(Guid alertId, CancellationToken cancellationToken);
}

public sealed class AlertService(
    IPredictiveMaintenanceDbContext db,
    IPredictionService predictionService,
    IAlertNotifier notifier) : IAlertService
{
    public async Task<int> EvaluateAsync(CancellationToken cancellationToken)
    {
        var parts = await db.MachineParts
            .Include(p => p.Telemetry)
            .Include(p => p.Alerts)
            .Include(p => p.ControllerUnit)
            .ThenInclude(c => c.Machine)
            .ThenInclude(m => m.Company)
            .ToListAsync(cancellationToken);

        var created = 0;
        foreach (var part in parts)
        {
            var prediction = await predictionService.PredictAsync(part, cancellationToken);
            if (prediction.Severity == AlertSeverity.Info)
            {
                continue;
            }

            var existing = part.Alerts
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefault(a => a.Status != AlertStatus.Resolved);
            var message = $"{part.ControllerUnit.Machine.Company.Name}: {part.Name} on {part.ControllerUnit.Machine.Name} is predicted due by {prediction.PredictedDueDate}.";
            if (existing is null)
            {
                db.MaintenanceAlerts.Add(new MaintenanceAlert
                {
                    MachinePartId = part.Id,
                    Severity = prediction.Severity,
                    Status = AlertStatus.Open,
                    PredictedDueDate = prediction.PredictedDueDate,
                    HealthScore = prediction.HealthScore,
                    Message = message,
                    ReasonCodes = string.Join(" | ", prediction.ReasonCodes)
                });
                created++;
            }
            else
            {
                existing.Severity = prediction.Severity;
                existing.PredictedDueDate = prediction.PredictedDueDate;
                existing.HealthScore = prediction.HealthScore;
                existing.Message = message;
                existing.ReasonCodes = string.Join(" | ", prediction.ReasonCodes);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<bool> AcknowledgeAsync(Guid alertId, CancellationToken cancellationToken)
    {
        var alert = await db.MaintenanceAlerts.FindAsync([alertId], cancellationToken);
        if (alert is null)
        {
            return false;
        }

        alert.Status = AlertStatus.Acknowledged;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> NotifyAsync(Guid alertId, CancellationToken cancellationToken)
    {
        var alert = await db.MaintenanceAlerts
            .Include(a => a.MachinePart)
            .ThenInclude(p => p.ControllerUnit)
            .ThenInclude(c => c.Machine)
            .ThenInclude(m => m.Company)
            .SingleOrDefaultAsync(a => a.Id == alertId, cancellationToken);

        if (alert is null)
        {
            return false;
        }

        await notifier.NotifyAsync(alert, cancellationToken);
        alert.Status = AlertStatus.Notified;
        alert.NotificationChannel = notifier.ChannelName;
        alert.NotifiedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
