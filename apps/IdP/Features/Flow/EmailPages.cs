namespace IdP.Features.Flow;

/// <summary>Confirms an e-mail address from the link in the verification e-mail; with <c>email</c>, changes it too.</summary>
public sealed record VerifyEmailPage() : GetHandlerAsync<VerifyEmailPage.Request>("/verify-email")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromQuery] string? UserId,
        [FromQuery] string? Token,
        [FromQuery] string? Email,
        [FromQuery] string? ReturnUrl,
        [FromServices] UserManager<IdPUser> Users,
        [FromServices] IEventService Events,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var options = request.Options.Value;
        var user = request.UserId == null ? null : await request.Users.FindByIdAsync(request.UserId);
        if (user == null || request.Token == null)
        {
            return FlowPages.Message(request.Spa, options, "Invalid link", "This verification link is not valid.", "/login", "Go to sign in", error: true);
        }

        var result = request.Email == null
            ? await request.Users.ConfirmEmailAsync(user, request.Token)
            : await request.Users.ChangeEmailAsync(user, request.Email, request.Token);

        if (!result.Succeeded)
        {
            return FlowPages.Message(request.Spa, options, "Invalid link", "This verification link has expired or was already used.", "/login", "Go to sign in", error: true);
        }

        await request.Events.RaiseAsync(new AuditEvent("E-mail verified", user.Id, user.UserName, user.Id, request.Email ?? user.Email ?? string.Empty));
        await SecurityStampValidation.RefreshAsync(request.HttpContext);
        var signedIn = request.HttpContext.User.Identity?.IsAuthenticated == true;
        return FlowPages.Message(request.Spa, options, "E-mail verified", $"{request.Email ?? user.Email} is verified.",
            signedIn ? "/account" : FlowPages.LoginUrl(request.ReturnUrl), signedIn ? "Go to my account" : "Sign in");
    }
}

/// <summary>Asks for the account's e-mail and sends the password reset link. The answer is the same whether the account exists or not.</summary>
public sealed record ForgotPasswordPage() : GetHandler<ForgotPasswordPage.Request>("/forgot-password")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] IAntiforgery Antiforgery,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] SpaPages Spa);

    protected override IResult Handle(Request request)
        => request.Spa.Render("forgot-password", "Reset your password", new
        {
            antiforgery = AntiforgeryModel.For(request.HttpContext, request.Antiforgery),
            product = FlowPages.Product(request.Options.Value)
        });
}

public sealed record ForgotPasswordForm() : PostHandlerAsync<ForgotPasswordForm.Request>("/forgot-password")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromForm] string? Email,
        [FromServices] UserManager<IdPUser> Users,
        [FromServices] AccountEmails Emails,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim();
        if (!string.IsNullOrEmpty(email))
        {
            var user = await request.Users.FindByEmailAsync(email) ?? await request.Users.FindByNameAsync(email);
            if (user is { Enabled: true, Email: not null })
            {
                await request.Emails.SendPasswordResetAsync(request.HttpContext, user);
            }
        }

        return FlowPages.Message(request.Spa, request.Options.Value, "Check your e-mail",
            "If an account exists for that address, we sent it a link to choose a new password.", "/login", "Back to sign in");
    }
}

/// <summary>Chooses a new password from the link in the reset e-mail.</summary>
public sealed record ResetPasswordPage() : GetHandler<ResetPasswordPage.Request>("/reset-password")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromQuery] string? UserId,
        [FromQuery] string? Token,
        [FromServices] IAntiforgery Antiforgery,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] SpaPages Spa);

    protected override IResult Handle(Request request)
    {
        if (request.UserId == null || request.Token == null)
        {
            return FlowPages.Message(request.Spa, request.Options.Value, "Invalid link", "This link is not valid.", "/forgot-password", "Ask for a new one", error: true);
        }

        return Render(request.HttpContext, request.Spa, request.Antiforgery, request.Options.Value, request.UserId, request.Token, null);
    }

    public static IResult Render(HttpContext http, SpaPages spa, IAntiforgery antiforgery, IdPOptions options, string userId, string token, string? error)
        => spa.Render("reset-password", "Choose a new password", new
        {
            userId,
            token,
            error,
            passwordLength = options.Password.RequiredLength,
            antiforgery = AntiforgeryModel.For(http, antiforgery),
            product = FlowPages.Product(options)
        });
}

public sealed record ResetPasswordForm() : PostHandlerAsync<ResetPasswordForm.Request>("/reset-password")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromForm] string? UserId,
        [FromForm] string? Token,
        [FromForm] string? Password,
        [FromServices] UserManager<IdPUser> Users,
        [FromServices] IEventService Events,
        [FromServices] IAntiforgery Antiforgery,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var options = request.Options.Value;
        var user = request.UserId == null ? null : await request.Users.FindByIdAsync(request.UserId);
        if (user == null || request.Token == null)
        {
            return FlowPages.Message(request.Spa, options, "Invalid link", "This link is not valid.", "/forgot-password", "Ask for a new one", error: true);
        }

        if (string.IsNullOrEmpty(request.Password))
        {
            return ResetPasswordPage.Render(request.HttpContext, request.Spa, request.Antiforgery, options, user.Id, request.Token, "Type the new password.");
        }

        var result = await request.Users.ResetPasswordAsync(user, request.Token, request.Password);
        if (!result.Succeeded)
        {
            var expired = result.Errors.Any(e => e.Code == "InvalidToken");
            return expired
                ? FlowPages.Message(request.Spa, options, "Link expired", "This link has expired or was already used.", "/forgot-password", "Ask for a new one", error: true)
                : ResetPasswordPage.Render(request.HttpContext, request.Spa, request.Antiforgery, options, user.Id, request.Token, string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        if (!user.EmailConfirmed)
        {
            // Following the link proves the address.
            user.EmailConfirmed = true;
            await request.Users.UpdateAsync(user);
        }

        if (await request.Users.IsLockedOutAsync(user))
        {
            await request.Users.SetLockoutEndDateAsync(user, null);
        }

        await request.Events.RaiseAsync(new AuditEvent("Password reset", user.Id, user.UserName, user.Id, "From the e-mail link"));
        return FlowPages.Message(request.Spa, options, "Password changed", "Sign in with your new password.", "/login", "Sign in");
    }
}
