namespace PredictiveMaintenance.Application.Interfaces;

public interface IEmailSender
{
    Task SendAsync(
        IReadOnlyCollection<string> recipients,
        string subject,
        string body,
        CancellationToken cancellationToken);
}
