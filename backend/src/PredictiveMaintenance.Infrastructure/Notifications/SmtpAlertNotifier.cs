using PredictiveMaintenance.Application.Interfaces;
using PredictiveMaintenance.Domain.Entities;

namespace PredictiveMaintenance.Infrastructure.Notifications;

public sealed class SmtpAlertNotifier(IEmailSender emailSender) : IAlertNotifier
{
    public string ChannelName => "smtp";

    public async Task NotifyAsync(MaintenanceAlert alert, CancellationToken cancellationToken)
    {
        var company = alert.MachinePart.ControllerUnit.Machine.Company;
        var machine = alert.MachinePart.ControllerUnit.Machine;
        var body = $"""
            Hello {company.ContactName},

            {alert.Message}

            Machine: {machine.Name}
            Part: {alert.MachinePart.Name} ({alert.MachinePart.Type})
            Predicted due date: {alert.PredictedDueDate}
            Health score: {alert.HealthScore}
            Reason: {alert.ReasonCodes}

            This is a local showcase notification.
            """;

        await emailSender.SendAsync(
            [company.ContactEmail],
            $"Predictive maintenance alert: {alert.MachinePart.Name}",
            body,
            cancellationToken);
    }
}
