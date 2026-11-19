namespace IdP.Features.Flow;

/// <summary>Self-registration, when <c>IdP:Registration:Enabled</c>. The new user signs in at once, or after verifying the e-mail.</summary>
public sealed record RegisterPage() : GetHandlerAsync<RegisterPage.Request>("/register")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromQuery] string? ReturnUrl,
        [FromServices] IAntiforgery Antiforgery,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] SpaPages Spa);

    protected override Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (!request.Options.Value.Registration.Enabled)
        {
            return Task.FromResult(Results.NotFound());
        }

        return Task.FromResult(Render(request.HttpContext, request.Spa, request.Antiforgery, request.Options.Value, request.ReturnUrl, null, null));
    }

    public static IResult Render(HttpContext http, SpaPages spa, IAntiforgery antiforgery, IdPOptions options, string? returnUrl, string? error, RegisterForm.Request? values)
        => spa.Render("register", "Create an account", new
        {
            returnUrl,
            error,
            values = values == null ? null : new { username = values.Value.Username, email = values.Value.Email, firstName = values.Value.FirstName, lastName = values.Value.LastName },
            requireEmailVerification = options.Registration.RequireEmailVerification,
            passwordLength = options.Password.RequiredLength,
            antiforgery = AntiforgeryModel.For(http, antiforgery),
            product = FlowPages.Product(options)
        });
}

public sealed record RegisterForm() : PostHandlerAsync<RegisterForm.Request>("/register")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromForm] string? Username,
        [FromForm] string? Email,
        [FromForm] string? Password,
        [FromForm] string? FirstName,
        [FromForm] string? LastName,
        [FromForm] string? ReturnUrl,
        [FromServices] UserManager<IdPUser> Users,
        [FromServices] IdPMongoContext Db,
        [FromServices] IEventService Events,
        [FromServices] AccountEmails Emails,
        [FromServices] IOidcServerInteractionService Interaction,
        [FromServices] IAntiforgery Antiforgery,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var options = request.Options.Value;
        if (!options.Registration.Enabled)
        {
            return Results.NotFound();
        }

        var http = request.HttpContext;
        var username = request.Username?.Trim();
        var email = request.Email?.Trim();
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(request.Password))
        {
            return RegisterPage.Render(http, request.Spa, request.Antiforgery, options, request.ReturnUrl, "User name, e-mail and password are required.", request);
        }

        if (await request.Users.FindByEmailAsync(email) != null)
        {
            return RegisterPage.Render(http, request.Spa, request.Antiforgery, options, request.ReturnUrl, "An account with that e-mail already exists.", request);
        }

        var user = new IdPUser { UserName = username, Email = email, FirstName = request.FirstName?.Trim(), LastName = request.LastName?.Trim() };
        var result = await request.Users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return RegisterPage.Render(http, request.Spa, request.Antiforgery, options, request.ReturnUrl, string.Join(" ", result.Errors.Select(e => e.Description)), request);
        }

        await request.Events.RaiseAsync(new AuditEvent("User registered", user.Id, user.UserName, user.Id, "Self-registration"));
        await request.Emails.SendVerificationAsync(http, user, request.ReturnUrl);

        if (options.Registration.RequireEmailVerification)
        {
            return FlowPages.Message(request.Spa, options, "Check your e-mail",
                $"We sent a link to {email}. Open it to verify your address, then sign in.", FlowPages.LoginUrl(request.ReturnUrl), "Back to sign in");
        }

        await SignIn.WithPasswordAsync(http, user, request.Db, request.Events);
        var context = request.ReturnUrl == null ? null : await request.Interaction.GetAuthorizationContextAsync(request.ReturnUrl);
        return SignIn.Return(request.ReturnUrl, context);
    }
}

/// <summary>Builds and sends the e-mails with links back to this server.</summary>
public sealed class AccountEmails(UserManager<IdPUser> users, IEmailSender sender, EmailTemplates templates)
{
    public async Task SendVerificationAsync(HttpContext http, IdPUser user, string? returnUrl = null)
    {
        if (string.IsNullOrEmpty(user.Email))
        {
            return;
        }

        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        var link = Link(http, "/verify-email", ("userId", user.Id), ("token", token), ("returnUrl", returnUrl));
        var (subject, html, text) = templates.VerifyEmail(user.DisplayName, link);
        await sender.SendAsync(user.Email, subject, html, text, http.RequestAborted);
    }

    public async Task SendEmailChangeAsync(HttpContext http, IdPUser user, string newEmail)
    {
        var token = await users.GenerateChangeEmailTokenAsync(user, newEmail);
        var link = Link(http, "/verify-email", ("userId", user.Id), ("token", token), ("email", newEmail));
        var (subject, html, text) = templates.VerifyEmail(user.DisplayName, link);
        await sender.SendAsync(newEmail, subject, html, text, http.RequestAborted);
    }

    public async Task SendPasswordResetAsync(HttpContext http, IdPUser user, bool invitation = false)
    {
        if (string.IsNullOrEmpty(user.Email))
        {
            return;
        }

        var token = await users.GeneratePasswordResetTokenAsync(user);
        var link = Link(http, "/reset-password", ("userId", user.Id), ("token", token));
        var (subject, html, text) = invitation ? templates.Invitation(user.DisplayName, link) : templates.ResetPassword(user.DisplayName, link);
        await sender.SendAsync(user.Email, subject, html, text, http.RequestAborted);
    }

    private static string Link(HttpContext http, string path, params (string Name, string? Value)[] query)
    {
        var builder = new QueryString();
        foreach (var (name, value) in query.Where(q => q.Value != null))
        {
            builder = builder.Add(name, value!);
        }

        return MinApiLib.Oidc.Server.Core.Extensions.HttpContextExtensions.GetOidcServerBaseUrl(http).TrimEnd('/') + path + builder.ToUriComponent();
    }
}
