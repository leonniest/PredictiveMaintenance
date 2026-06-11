using Microsoft.EntityFrameworkCore;
using PredictiveMaintenance.Application.Auth;
using PredictiveMaintenance.Application.Interfaces;
using PredictiveMaintenance.Application.Support;
using PredictiveMaintenance.Domain.Entities;
using PredictiveMaintenance.Domain.Enums;

namespace PredictiveMaintenance.Application.Services;

public interface ISupportService
{
    Task<IReadOnlyList<SupportSettingsDto>> GetSettingsAsync(UserContext user, CancellationToken cancellationToken);
    Task<SupportSettingsDto?> UpdateSettingsAsync(UserContext user, UpdateSupportSettingsRequest request, CancellationToken cancellationToken);
    Task<DispatchTechnicianResultDto?> DispatchTechnicianAsync(UserContext user, DispatchTechnicianRequest request, CancellationToken cancellationToken);
}

public sealed class SupportService(
    IPredictiveMaintenanceDbContext db,
    IEmailSender emailSender) : ISupportService
{
    public async Task<IReadOnlyList<SupportSettingsDto>> GetSettingsAsync(UserContext user, CancellationToken cancellationToken)
    {
        var query = db.ResellerSupportSettings
            .Include(s => s.Company)
            .Where(s => user.Role == UserRole.Admin || s.CompanyId == user.CompanyId)
            .OrderBy(s => s.Company.Name);

        return await query.Select(s => ToDto(s)).ToListAsync(cancellationToken);
    }

    public async Task<SupportSettingsDto?> UpdateSettingsAsync(UserContext user, UpdateSupportSettingsRequest request, CancellationToken cancellationToken)
    {
        if (user.Role != UserRole.Admin && request.CompanyId != user.CompanyId)
        {
            return null;
        }

        var settings = await db.ResellerSupportSettings
            .Include(s => s.Company)
            .SingleOrDefaultAsync(s => s.CompanyId == request.CompanyId, cancellationToken);

        if (settings is null)
        {
            var company = await db.Companies.SingleOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);
            if (company is null)
            {
                return null;
            }

            settings = new ResellerSupportSettings { CompanyId = request.CompanyId, Company = company };
            db.ResellerSupportSettings.Add(settings);
        }

        settings.SupportDepartmentEmail = request.SupportDepartmentEmail.Trim();
        settings.TechnicianDispatchEmail = request.TechnicianDispatchEmail.Trim();
        settings.EscalationPhone = request.EscalationPhone.Trim();
        settings.DefaultSlaHours = Math.Clamp(request.DefaultSlaHours, 1, 168);
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(settings);
    }

    public async Task<DispatchTechnicianResultDto?> DispatchTechnicianAsync(UserContext user, DispatchTechnicianRequest request, CancellationToken cancellationToken)
    {
        var machine = await db.Machines
            .Include(m => m.Company)
            .ThenInclude(c => c.SupportSettings)
            .Include(m => m.Controller)
            .ThenInclude(c => c.Parts)
            .ThenInclude(p => p.Alerts)
            .SingleOrDefaultAsync(m => m.Id == request.MachineId, cancellationToken);

        if (machine is null || (user.Role != UserRole.Admin && machine.CompanyId != user.CompanyId))
        {
            return null;
        }

        var alert = request.AlertId is null
            ? null
            : machine.Controller.Parts.SelectMany(p => p.Alerts).SingleOrDefault(a => a.Id == request.AlertId);

        if (request.AlertId is not null && alert is null)
        {
            return null;
        }

        var settings = machine.Company.SupportSettings;
        var recipients = new[]
            {
                settings?.SupportDepartmentEmail,
                settings?.TechnicianDispatchEmail
            }
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recipients.Count == 0)
        {
            recipients.Add(machine.Company.ContactEmail);
        }

        var dueParts = machine.Controller.Parts
            .SelectMany(p => p.Alerts.Select(a => new { Part = p, Alert = a }))
            .Where(x => x.Alert.Status is AlertStatus.Open or AlertStatus.Notified)
            .OrderBy(x => x.Alert.PredictedDueDate)
            .Take(6)
            .ToList();

        var body = $"""
        Technician dispatch requested by {user.Email} ({user.Role}).

        Reseller: {machine.Company.Name}
        Machine: {machine.Name}
        Location: {machine.Location}
        Controller: {machine.Controller.Kind} {machine.Controller.Model} ({machine.Controller.SerialNumber})
        SLA target: {settings?.DefaultSlaHours ?? 24} hours

        Selected alert: {(alert is null ? "Machine-level dispatch" : $"{alert.Severity} / due {alert.PredictedDueDate} / health {alert.HealthScore}")}
        Note: {request.Note}

        Open machine alerts:
        {string.Join(Environment.NewLine, dueParts.Select(x => $"- {x.Part.Name} ({x.Part.Type}): {x.Alert.Severity}, due {x.Alert.PredictedDueDate}, first alerted {x.Alert.CreatedAt:yyyy-MM-dd HH:mm} UTC, health {x.Alert.HealthScore}"))}
        """;

        await emailSender.SendAsync(
            recipients,
            $"Technician dispatch: {machine.Name}",
            body,
            cancellationToken);

        return new DispatchTechnicianResultDto(machine.Id, alert?.Id, recipients, DateTimeOffset.UtcNow);
    }

    private static SupportSettingsDto ToDto(ResellerSupportSettings settings)
        => new(
            settings.CompanyId,
            settings.Company.Name,
            settings.SupportDepartmentEmail,
            settings.TechnicianDispatchEmail,
            settings.EscalationPhone,
            settings.DefaultSlaHours,
            settings.UpdatedAt);
}
