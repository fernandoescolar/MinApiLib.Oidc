using System.Net.Mail;

namespace IdP.Infrastructure.Email;

/// <summary>Sends e-mails through the SMTP server of <c>IdP:Email:Smtp</c>.</summary>
public sealed class SmtpEmailSender(IOptions<IdPOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, string textBody, CancellationToken cancellationToken = default)
    {
        var email = options.Value.Email;
        var smtp = email.Smtp;

        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = true,
            Credentials = string.IsNullOrEmpty(smtp.UserName) ? null : new NetworkCredential(smtp.UserName, smtp.Password)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(email.From, email.FromName ?? options.Value.Name),
            Subject = subject,
            Body = textBody,
            IsBodyHtml = false
        };
        message.To.Add(to);
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html"));

        await client.SendMailAsync(message, cancellationToken);
        logger.LogInformation("E-mail \"{Subject}\" sent to {To}", subject, to);
    }
}

/// <summary>Writes the e-mails to the log instead of sending them: for development, and when no SMTP server is configured.</summary>
public sealed class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, string textBody, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("No SMTP server is configured (IdP:Email:Smtp:Host). E-mail to {To}: {Subject}\n{Body}", to, subject, textBody);
        return Task.CompletedTask;
    }
}
