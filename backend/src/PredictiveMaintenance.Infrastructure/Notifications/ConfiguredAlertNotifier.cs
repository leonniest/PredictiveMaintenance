using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PredictiveMaintenance.Application.Interfaces;
using PredictiveMaintenance.Domain.Entities;

namespace PredictiveMaintenance.Infrastructure.Notifications;

public sealed class ConfiguredAlertNotifier(
    IOptions<NotificationOptions> options,
    IServiceProvider serviceProvider) : IAlertNotifier
{
    public string ChannelName => Selected.ChannelName;

    public Task NotifyAsync(MaintenanceAlert alert, CancellationToken cancellationToken)
        => Selected.NotifyAsync(alert, cancellationToken);

    private IAlertNotifier Selected
    {
        get
        {
            var mode = options.Value.Mode.Trim();
            return mode.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase)
                ? serviceProvider.GetRequiredKeyedService<IAlertNotifier>("rabbitmq")
                : serviceProvider.GetRequiredKeyedService<IAlertNotifier>("smtp");
        }
    }
}
