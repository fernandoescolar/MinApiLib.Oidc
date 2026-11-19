namespace IdP.Infrastructure.Email;

/// <summary>Sends the e-mails of the identity provider: verification, password reset, invitations.</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, string textBody, CancellationToken cancellationToken = default);
}
