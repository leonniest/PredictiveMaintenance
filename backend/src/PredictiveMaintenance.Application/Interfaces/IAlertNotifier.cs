using PredictiveMaintenance.Domain.Entities;

namespace PredictiveMaintenance.Application.Interfaces;

public interface IAlertNotifier
{
    string ChannelName { get; }
    Task NotifyAsync(MaintenanceAlert alert, CancellationToken cancellationToken);
}
