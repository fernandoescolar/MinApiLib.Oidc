namespace IdP.Infrastructure.Email;

/// <summary>The e-mails the identity provider sends, as plain text and a small HTML version.</summary>
public sealed class EmailTemplates(IOptions<IdPOptions> options)
{
    private string Name => options.Value.Name;

    public (string Subject, string Html, string Text) VerifyEmail(string userName, string link)
        => Build($"Verify your e-mail address for {Name}",
            $"Hello {userName},",
            "Confirm this e-mail address for your account by opening the link below.",
            link, "Verify my e-mail");

    public (string Subject, string Html, string Text) ResetPassword(string userName, string link)
        => Build($"Reset your {Name} password",
            $"Hello {userName},",
            "Somebody asked to reset the password of your account. If it was you, open the link below; otherwise ignore this e-mail.",
            link, "Choose a new password");

    public (string Subject, string Html, string Text) Invitation(string userName, string link)
        => Build($"Your {Name} account",
            $"Hello {userName},",
            "An account has been created for you. Open the link below to choose your password.",
            link, "Set my password");

    private (string Subject, string Html, string Text) Build(string subject, string greeting, string paragraph, string link, string action)
    {
        var text = $"{greeting}\n\n{paragraph}\n\n{link}\n\n{Name}";
        var html = $$"""
            <!doctype html>
            <html><body style="font-family: system-ui, sans-serif; color: #1e293b; line-height: 1.5">
            <p>{{WebUtility.HtmlEncode(greeting)}}</p>
            <p>{{WebUtility.HtmlEncode(paragraph)}}</p>
            <p><a href="{{WebUtility.HtmlEncode(link)}}" style="display:inline-block;background:#4f46e5;color:#fff;padding:10px 18px;border-radius:8px;text-decoration:none">{{WebUtility.HtmlEncode(action)}}</a></p>
            <p style="color:#64748b;font-size:12px">If the button does not work, copy this address into your browser:<br>{{WebUtility.HtmlEncode(link)}}</p>
            <p style="color:#64748b;font-size:12px">{{WebUtility.HtmlEncode(Name)}}</p>
            </body></html>
            """;
        return (subject, html, text);
    }
}
