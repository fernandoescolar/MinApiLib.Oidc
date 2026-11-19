namespace IdP.Features.Flow;

/// <summary>
/// Starts a sign-in with an external provider: a challenge to its scheme, which comes back to <see cref="ExternalCallback"/>
/// through the external cookie. With <c>mode=link</c>, the signed-in user links the external account to theirs instead.
/// </summary>
public sealed record ExternalChallenge() : GetHandlerAsync<ExternalChallenge.Request>("/external/challenge")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromQuery] string? Scheme,
        [FromQuery] string? ReturnUrl,
        [FromQuery] string? Mode,
        [FromServices] IAuthenticationSchemeProvider Schemes,
        [FromServices] IOidcServerInteractionService Interaction);

    public static string Url(string alias, string? returnUrl, bool link = false)
        => $"/external/challenge?scheme={Uri.EscapeDataString(alias)}{(returnUrl == null ? string.Empty : "&returnUrl=" + Uri.EscapeDataString(returnUrl))}{(link ? "&mode=link" : string.Empty)}";

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (request.Scheme == null || await request.Schemes.GetSchemeAsync(request.Scheme) == null)
        {
            return Results.Redirect("/login?error=" + Uri.EscapeDataString("Unknown identity provider."));
        }

        var returnUrl = request.ReturnUrl != null && (request.Interaction.IsValidReturnUrl(request.ReturnUrl) || SignIn.IsLocal(request.ReturnUrl)) ? request.ReturnUrl : "/account";
        var link = request.Mode == "link" && request.HttpContext.User.Identity?.IsAuthenticated == true;

        var properties = new AuthenticationProperties
        {
            RedirectUri = "/external/callback",
            Items =
            {
                ["scheme"] = request.Scheme,
                ["returnUrl"] = returnUrl,
                ["mode"] = link ? "link" : "signin",
                ["linkTo"] = link ? request.HttpContext.User.GetSubjectId() : string.Empty
            }
        };

        return Results.Challenge(properties, [request.Scheme]);
    }
}

/// <summary>
/// The external provider has authenticated the user. Finds the local account linked to the external identity, or
/// creates one (when the provider allows it), signs the user in and returns to the authorize request.
/// </summary>
public sealed record ExternalCallback() : GetHandlerAsync<ExternalCallback.Request>("/external/callback")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] UserManager<IdPUser> Users,
        [FromServices] IdPMongoContext Db,
        [FromServices] IEventService Events,
        [FromServices] IOidcServerInteractionService Interaction,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var http = request.HttpContext;
        var result = await http.AuthenticateAsync(OidcServerConstants.ExternalCookieAuthenticationScheme);
        if (result is not { Succeeded: true, Principal: var external, Properties: var properties })
        {
            return Results.Redirect("/login?error=" + Uri.EscapeDataString("The external sign-in failed."));
        }

        var scheme = properties.Items["scheme"]!;
        var returnUrl = properties.Items["returnUrl"];
        var link = properties.Items["mode"] == "link";
        var providerKey = external.FindFirst(JwtClaimTypes.Subject)?.Value ?? external.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var provider = await request.Db.IdentityProviders.Find(p => p.Alias == scheme).FirstOrDefaultAsync(cancellationToken);

        await http.SignOutAsync(OidcServerConstants.ExternalCookieAuthenticationScheme);

        if (providerKey == null || provider == null)
        {
            return Results.Redirect("/login?error=" + Uri.EscapeDataString("The external provider did not identify the user."));
        }

        var displayName = provider.DisplayName;
        var email = external.FindFirst(JwtClaimTypes.Email)?.Value ?? external.FindFirst(ClaimTypes.Email)?.Value;
        var emailVerified = provider.TrustEmail || string.Equals(external.FindFirst(JwtClaimTypes.EmailVerified)?.Value, "true", StringComparison.OrdinalIgnoreCase);
        var existing = await request.Users.FindByLoginAsync(scheme, providerKey);

        if (link)
        {
            var current = await request.Users.FindByIdAsync(properties.Items["linkTo"] ?? string.Empty);
            if (current == null)
            {
                return FlowPages.RedirectToLogin(returnUrl);
            }

            if (existing != null && existing.Id != current.Id)
            {
                return Results.Redirect("/account/linked?error=" + Uri.EscapeDataString($"That {displayName} account is linked to another user."));
            }

            await request.Users.AddLoginAsync(current, new UserLoginInfo(scheme, providerKey, displayName));
            await request.Events.RaiseAsync(new AuditEvent("External account linked", current.Id, current.UserName, current.Id, $"{displayName} linked"));
            return Results.Redirect(SignIn.IsLocal(returnUrl) ? returnUrl! : "/account/linked");
        }

        var user = existing;
        if (user == null && provider.TrustEmail && email != null)
        {
            user = await request.Users.FindByEmailAsync(email);
            if (user != null)
            {
                await request.Users.AddLoginAsync(user, new UserLoginInfo(scheme, providerKey, displayName));
            }
        }

        if (user == null)
        {
            if (!provider.AutoProvision)
            {
                await request.Events.RaiseAsync(new UserLoginFailureEvent($"{scheme}:{providerKey}", "no linked account"));
                return Results.Redirect("/login?error=" + Uri.EscapeDataString($"No account is linked to that {displayName} identity. Sign in and link it from your account."));
            }

            user = await ProvisionAsync(request.Users, scheme, providerKey, displayName, email, emailVerified, external);
            if (user == null)
            {
                return Results.Redirect("/login?error=" + Uri.EscapeDataString("The account could not be created."));
            }

            await request.Events.RaiseAsync(new AuditEvent("User provisioned", user.Id, user.UserName, scheme, $"Created from {displayName}"));
        }

        if (!user.Enabled || await request.Users.IsLockedOutAsync(user))
        {
            return Results.Redirect("/login?error=" + Uri.EscapeDataString("This account cannot sign in right now."));
        }

        await SignIn.WithExternalProviderAsync(http, user, scheme, request.Db, request.Events);
        var context = returnUrl == null ? null : await request.Interaction.GetAuthorizationContextAsync(returnUrl);
        return SignIn.Return(returnUrl, context);
    }

    private static async Task<IdPUser?> ProvisionAsync(UserManager<IdPUser> users, string scheme, string providerKey, string displayName, string? email, bool emailVerified, ClaimsPrincipal external)
    {
        var preferred = external.FindFirst(JwtClaimTypes.PreferredUserName)?.Value ?? email ?? $"{scheme}_{providerKey}";
        var userName = new string(preferred.Where(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '.' or '_' or '@' or '+').ToArray());
        if (userName.Length == 0)
        {
            userName = $"{scheme}_{Guid.NewGuid():N}";
        }

        if (await users.FindByNameAsync(userName) != null)
        {
            userName = $"{userName}_{Guid.NewGuid().ToString("N")[..6]}";
        }

        var user = new IdPUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = email != null && emailVerified,
            FirstName = external.FindFirst(JwtClaimTypes.GivenName)?.Value ?? external.FindFirst(ClaimTypes.GivenName)?.Value,
            LastName = external.FindFirst(JwtClaimTypes.FamilyName)?.Value ?? external.FindFirst(ClaimTypes.Surname)?.Value
        };

        if (user.FirstName == null && user.LastName == null && external.FindFirst(JwtClaimTypes.Name)?.Value is { } name)
        {
            user.FirstName = name;
        }

        if (external.FindFirst(JwtClaimTypes.Picture)?.Value is { } picture)
        {
            user.Attributes[JwtClaimTypes.Picture] = picture;
        }

        if (external.FindFirst(JwtClaimTypes.Locale)?.Value is { } locale)
        {
            user.Attributes[JwtClaimTypes.Locale] = locale;
        }

        var result = await users.CreateAsync(user);
        if (!result.Succeeded)
        {
            return null;
        }

        await users.AddLoginAsync(user, new UserLoginInfo(scheme, providerKey, displayName));
        return user;
    }
}
