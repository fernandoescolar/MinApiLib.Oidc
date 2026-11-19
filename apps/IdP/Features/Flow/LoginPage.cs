namespace IdP.Features.Flow;

/// <summary>
/// The login page. The authorize endpoint redirects here with the authorize request in <c>returnUrl</c>. The user
/// signs in with a password (and a second factor, when enabled), a passkey, or an external provider; when the client
/// asks for <c>acr_values=phr</c>, only with a passkey.
/// </summary>
public sealed record LoginPage() : GetHandlerAsync<LoginPage.Request>("/login")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromQuery] string? ReturnUrl,
        [FromQuery] string? Error,
        [FromQuery] string? Message,
        [FromServices] LoginRenderer Renderer,
        [FromServices] IOidcServerInteractionService Interaction);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var context = request.ReturnUrl == null ? null : await request.Interaction.GetAuthorizationContextAsync(request.ReturnUrl);
        if (context == null && request.ReturnUrl != null && !SignIn.IsLocal(request.ReturnUrl))
        {
            return Results.Redirect("/login");
        }

        // Signed in already, and nothing asks for a fresh sign-in: straight back.
        if (request.HttpContext.User.Identity?.IsAuthenticated == true && context == null && request.Error == null)
        {
            return SignIn.Return(request.ReturnUrl, context);
        }

        return await request.Renderer.RenderAsync(request.HttpContext, request.ReturnUrl, context, request.Error, request.Message);
    }
}

/// <summary>Renders the login page with the client, the providers and the messages.</summary>
public sealed class LoginRenderer(SpaPages spa, IAntiforgery antiforgery, IOptions<IdPOptions> options, DynamicSchemeManager providers)
{
    public async Task<IResult> RenderAsync(HttpContext http, string? returnUrl, AuthorizationRequest? context, string? error, string? message = null)
    {
        var external = await providers.GetEnabledAsync(http.RequestAborted);
        var allowed = context?.Client.IdentityProviderRestrictions is { Count: > 0 } restrictions
            ? external.Where(p => restrictions.Contains(p.Alias)).ToList()
            : external.Where(p => p.ShowOnLoginPage).ToList();

        // acr_values=idp:<alias> sends the user straight to that provider.
        if (context?.IdP is { } idp && allowed.Any(p => p.Alias == idp) && error == null)
        {
            return Results.Redirect(ExternalChallenge.Url(idp, returnUrl));
        }

        return spa.Render("login", "Sign in", new
        {
            returnUrl,
            clientName = context == null ? null : context.Client.ClientName ?? context.Client.ClientId,
            clientUri = context?.Client.ClientUri,
            logoUri = context?.Client.LogoUri,
            loginHint = context?.LoginHint,
            requiresPasskey = SignIn.RequiresPasskey(context),
            localLogin = context?.Client.EnableLocalLogin ?? true,
            registrationEnabled = options.Value.Registration.Enabled,
            externalProviders = allowed.Select(p => new { alias = p.Alias, displayName = p.DisplayName, url = ExternalChallenge.Url(p.Alias, returnUrl) }),
            error,
            message,
            antiforgery = AntiforgeryModel.For(http, antiforgery),
            product = FlowPages.Product(options.Value)
        });
    }
}

/// <summary>
/// Checks the password with ASP.NET Core Identity. Then signs the user in, or, when the account has a second factor,
/// goes to the second-factor page. Binding the form turns on antiforgery validation.
/// </summary>
public sealed record LoginForm() : PostHandlerAsync<LoginForm.Request>("/login")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromForm] string? Username,
        [FromForm] string? Password,
        [FromForm] string? ReturnUrl,
        [FromServices] UserManager<IdPUser> Users,
        [FromServices] IdPMongoContext Db,
        [FromServices] IEventService Events,
        [FromServices] IOidcServerInteractionService Interaction,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] MfaState Mfa,
        [FromServices] LoginRenderer Renderer);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var http = request.HttpContext;
        var context = request.ReturnUrl == null ? null : await request.Interaction.GetAuthorizationContextAsync(request.ReturnUrl);
        var clientId = context?.Client.ClientId;
        var username = request.Username?.Trim() ?? string.Empty;

        if (SignIn.RequiresPasskey(context))
        {
            return await request.Renderer.RenderAsync(http, request.ReturnUrl, context, "This application needs a passkey.");
        }

        if (context?.Client.EnableLocalLogin == false)
        {
            return await request.Renderer.RenderAsync(http, request.ReturnUrl, context, "This application does not accept a local sign-in.");
        }

        var user = username.Length == 0 ? null : await request.Users.FindByNameAsync(username) ?? (username.Contains('@') ? await request.Users.FindByEmailAsync(username) : null);
        if (user == null || string.IsNullOrEmpty(request.Password))
        {
            await request.Events.RaiseAsync(new UserLoginFailureEvent(username, "invalid credentials", clientId: clientId));
            return await request.Renderer.RenderAsync(http, request.ReturnUrl, context, "Invalid user name or password.");
        }

        if (!user.Enabled)
        {
            await request.Events.RaiseAsync(new UserLoginFailureEvent(username, "account disabled", clientId: clientId));
            return await request.Renderer.RenderAsync(http, request.ReturnUrl, context, "This account is disabled.");
        }

        if (await request.Users.IsLockedOutAsync(user))
        {
            await request.Events.RaiseAsync(new UserLoginFailureEvent(username, "locked out", clientId: clientId));
            return await request.Renderer.RenderAsync(http, request.ReturnUrl, context, "Too many failed attempts. The account is locked for a few minutes.");
        }

        if (!await request.Users.CheckPasswordAsync(user, request.Password))
        {
            await request.Users.AccessFailedAsync(user);
            await request.Events.RaiseAsync(new UserLoginFailureEvent(username, "invalid credentials", clientId: clientId));
            return await request.Renderer.RenderAsync(http, request.ReturnUrl, context, "Invalid user name or password.");
        }

        if (user.AccessFailedCount > 0)
        {
            await request.Users.ResetAccessFailedCountAsync(user);
        }

        if (request.Options.Value.Registration.RequireEmailVerification && !user.EmailConfirmed)
        {
            return await request.Renderer.RenderAsync(http, request.ReturnUrl, context, "Verify your e-mail address first: open the link we sent you.");
        }

        if (user.TwoFactorEnabled)
        {
            request.Mfa.Save(http, new MfaState.Pending(user.Id, request.ReturnUrl));
            return Results.Redirect("/login/mfa");
        }

        if (SignIn.RequiresMfa(context))
        {
            return await request.Renderer.RenderAsync(http, request.ReturnUrl, context,
                "This application needs a second factor. Add an authenticator app or a passkey in your account, then sign in again.");
        }

        await SignIn.WithPasswordAsync(http, user, request.Db, request.Events);
        return SignIn.Return(request.ReturnUrl, context);
    }
}

/// <summary>The second step of the sign-in for accounts with an authenticator app: a TOTP code, or a recovery code.</summary>
public sealed record MfaPage() : GetHandlerAsync<MfaPage.Request>("/login/mfa")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] MfaState Mfa,
        [FromServices] UserManager<IdPUser> Users,
        [FromServices] IAntiforgery Antiforgery,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var pending = request.Mfa.Peek(request.HttpContext);
        var user = pending == null ? null : await request.Users.FindByIdAsync(pending.UserId);
        if (user == null)
        {
            return Results.Redirect("/login");
        }

        return Render(request.HttpContext, request.Spa, request.Antiforgery, request.Options.Value, user, error: null);
    }

    public static IResult Render(HttpContext http, SpaPages spa, IAntiforgery antiforgery, IdPOptions options, IdPUser user, string? error)
        => spa.Render("mfa", "Second factor", new
        {
            userName = user.UserName,
            error,
            antiforgery = AntiforgeryModel.For(http, antiforgery),
            product = FlowPages.Product(options)
        });
}

public sealed record MfaForm() : PostHandlerAsync<MfaForm.Request>("/login/mfa")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromForm] string? Code,
        [FromForm] bool? Recovery,
        [FromServices] MfaState Mfa,
        [FromServices] UserManager<IdPUser> Users,
        [FromServices] IdPMongoContext Db,
        [FromServices] IEventService Events,
        [FromServices] IOidcServerInteractionService Interaction,
        [FromServices] IAntiforgery Antiforgery,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var http = request.HttpContext;
        var pending = request.Mfa.Peek(http);
        var user = pending == null ? null : await request.Users.FindByIdAsync(pending.UserId);
        if (user == null)
        {
            return Results.Redirect("/login");
        }

        var code = (request.Code ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        var recovery = request.Recovery == true;
        var valid = recovery
            ? (await request.Users.RedeemTwoFactorRecoveryCodeAsync(user, code)).Succeeded
            : await request.Users.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, code);

        if (!valid)
        {
            await request.Users.AccessFailedAsync(user);
            await request.Events.RaiseAsync(new UserLoginFailureEvent(user.UserName, recovery ? "invalid recovery code" : "invalid second factor"));
            if (await request.Users.IsLockedOutAsync(user))
            {
                request.Mfa.Clear(http);
                return Results.Redirect("/login?error=" + Uri.EscapeDataString("Too many failed attempts. The account is locked for a few minutes."));
            }

            return MfaPage.Render(http, request.Spa, request.Antiforgery, request.Options.Value, user, recovery ? "That recovery code is not valid." : "That code is not valid. Codes change every 30 seconds.");
        }

        request.Mfa.Clear(http);
        await request.Users.ResetAccessFailedCountAsync(user);
        await SignIn.WithPasswordAndTotpAsync(http, user, request.Db, request.Events);

        var context = pending!.ReturnUrl == null ? null : await request.Interaction.GetAuthorizationContextAsync(pending.ReturnUrl);
        return SignIn.Return(pending.ReturnUrl, context);
    }
}
