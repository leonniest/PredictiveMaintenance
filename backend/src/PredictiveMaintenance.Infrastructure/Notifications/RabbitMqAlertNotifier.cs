using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PredictiveMaintenance.Application.Interfaces;
using PredictiveMaintenance.Domain.Entities;
using RabbitMQ.Client;

namespace PredictiveMaintenance.Infrastructure.Notifications;

public sealed class RabbitMqAlertNotifier(IOptions<RabbitMqOptions> options) : IAlertNotifier
{
    public string ChannelName => "rabbitmq";

    public async Task NotifyAsync(MaintenanceAlert alert, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password
        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(settings.QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);

        var payload = JsonSerializer.Serialize(new
        {
            alert.Id,
            alert.Severity,
            alert.PredictedDueDate,
            alert.HealthScore,
            alert.Message,
            alert.ReasonCodes,
            Company = alert.MachinePart.ControllerUnit.Machine.Company.Name,
            ContactEmail = alert.MachinePart.ControllerUnit.Machine.Company.ContactEmail,
            Machine = alert.MachinePart.ControllerUnit.Machine.Name,
            Part = alert.MachinePart.Name,
            PartType = alert.MachinePart.Type.ToString()
        });

        var body = Encoding.UTF8.GetBytes(payload);
        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: settings.QueueName, body: body, cancellationToken: cancellationToken);
    }
}
