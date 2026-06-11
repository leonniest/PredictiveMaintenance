namespace PredictiveMaintenance.Infrastructure.Notifications;

public sealed class NotificationOptions
{
    public string Mode { get; set; } = "Smtp";
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public string From { get; set; } = "maintenance@showcase.local";
    public bool UseStartTls { get; set; }
}

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string QueueName { get; set; } = "predictive-maintenance.alerts";
}
