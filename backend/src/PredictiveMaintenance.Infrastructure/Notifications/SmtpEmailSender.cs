using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using PredictiveMaintenance.Application.Interfaces;

namespace PredictiveMaintenance.Infrastructure.Notifications;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    public async Task SendAsync(
        IReadOnlyCollection<string> recipients,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        if (recipients.Count == 0)
        {
            return;
        }

        var settings = options.Value;
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(settings.From));
        foreach (var recipient in recipients)
        {
            message.To.Add(MailboxAddress.Parse(recipient));
        }

        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            settings.Host,
            settings.Port,
            settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
            cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
