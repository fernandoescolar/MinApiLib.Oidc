using System.Text;
using System.Text.Encodings.Web;

namespace IdP.Features.Api.Account;

// The JSON API of the account console (/api/account): the signed-in user's profile, credentials, passkeys,
// applications and linked accounts. Cookie-authenticated, with the antiforgery header on state-changing calls.

public sealed record ProfileModel(
    string Id, string UserName, string? Email, bool EmailConfirmed, string? FirstName, string? LastName, string? PhoneNumber,
    IReadOnlyList<string> Roles, IReadOnlyList<string> Groups, bool HasPassword, bool TwoFactorEnabled, int RecoveryCodesLeft,
    IReadOnlyList<PasskeyModel> Passkeys, IReadOnlyList<LinkedAccountModel> Logins, DateTime CreatedAt, DateTime? LastSignInAt,
    IReadOnlyDictionary<string, string> Attributes);

public sealed record PasskeyModel(string Id, string? Name, DateTimeOffset CreatedAt, bool Synced, bool UserVerified);

public sealed record LinkedAccountModel(string Provider, string? DisplayName, string ProviderKey, DateTime LinkedAt);

public sealed record ApplicationModel(string ClientId, string ClientName, string? ClientUri, string? LogoUri, IReadOnlyList<string> Scopes, DateTime CreationTime, DateTime? Expiration, string? Description);

public static class AccountModels
{
    public static async Task<ProfileModel> ProfileAsync(IdPUser user, IdPMongoContext db, UserManager<IdPUser> users)
    {
        var groups = user.Groups.Count == 0 ? [] : await db.Groups.Find(g => user.Groups.Contains(g.Id)).ToListAsync();
        return new ProfileModel(
            user.Id, user.UserName, user.Email, user.EmailConfirmed, user.FirstName, user.LastName, user.PhoneNumber,
            user.Roles.Concat(groups.SelectMany(g => g.Roles)).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList(),
            groups.Select(g => g.Name).Order(StringComparer.OrdinalIgnoreCase).ToList(),
            user.PasswordHash != null, user.TwoFactorEnabled, await users.CountRecoveryCodesAsync(user),
            user.Passkeys.Select(Passkey).ToList(),
            user.Logins.Select(l => new LinkedAccountModel(l.LoginProvider, l.ProviderDisplayName, l.ProviderKey, l.LinkedAt)).ToList(),
            user.CreatedAt, user.LastSignInAt, user.Attributes);
    }

    public static PasskeyModel Passkey(PasskeyDocument p) => new(Base64Url.EncodeToString(p.CredentialId), p.Name, p.CreatedAt, p.IsBackedUp, p.IsUserVerified);

    public static async Task<IdPUser?> CurrentAsync(HttpContext http, UserManager<IdPUser> users)
        => http.User.Identity?.IsAuthenticated == true ? await users.FindByIdAsync(http.User.GetSubjectId()) : null;
}

public sealed record GetProfile() : GetHandlerAsync<GetProfile.Request>("/api/account/me")
{
    public readonly record struct Request(HttpContext HttpContext, [FromServices] UserManager<IdPUser> Users, [FromServices] IdPMongoContext Db);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireUser();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await AccountModels.CurrentAsync(request.HttpContext, request.Users);
        return user == null ? Results.Unauthorized() : Results.Ok(await AccountModels.ProfileAsync(user, request.Db, request.Users));
    }
}

public sealed record UpdateProfile() : PutHandlerAsync<UpdateProfile.Request>("/api/account/profile")
{
    public sealed record Body(string? FirstName, string? LastName, string? PhoneNumber, string? Email, Dictionary<string, string>? Attributes);

    public readonly record struct Request(HttpContext HttpContext, [FromBody] Body Body, [FromServices] UserManager<IdPUser> Users, [FromServices] IdPMongoContext Db,
        [FromServices] AccountEmails Emails, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireUser();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await AccountModels.CurrentAsync(request.HttpContext, request.Users);
        if (user == null)
        {
            return Results.Unauthorized();
        }

        user.FirstName = request.Body.FirstName?.Trim();
        user.LastName = request.Body.LastName?.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.Body.PhoneNumber) ? null : request.Body.PhoneNumber.Trim();
        if (request.Body.Attributes != null)
        {
            // Only the user's own free attributes; the identity claims have their own fields.
            foreach (var (key, value) in request.Body.Attributes.Where(a => a.Key is not (JwtClaimTypes.Role or "groups" or JwtClaimTypes.Subject)))
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    user.Attributes.Remove(key);
                }
                else
                {
                    user.Attributes[key] = value.Trim();
                }
            }
        }

        // The e-mail is checked before anything is saved, so a 400 means nothing changed.
        var newEmail = request.Body.Email?.Trim();
        var emailChangePending = !string.IsNullOrEmpty(newEmail) && !string.Equals(newEmail, user.Email, StringComparison.OrdinalIgnoreCase);
        if (emailChangePending && await request.Users.FindByEmailAsync(newEmail!) != null)
        {
            return ApiErrors.Validation("Another account uses that e-mail.", "email");
        }

        var result = await request.Users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return ApiErrors.Identity(result);
        }

        if (emailChangePending)
        {
            await request.Emails.SendEmailChangeAsync(request.HttpContext, user, newEmail!);
        }

        await request.Events.RaiseAsync(new AuditEvent("Profile updated", user.Id, user.UserName, user.Id, "By the user"));
        return Results.Ok(new { profile = await AccountModels.ProfileAsync(user, request.Db, request.Users), emailChangePending, pendingEmail = emailChangePending ? newEmail : null });
    }
}

public sealed record ResendVerification() : PostHandlerAsync<ResendVerification.Request>("/api/account/email/resend-verification")
{
    public readonly record struct Request(HttpContext HttpContext, [FromServices] UserManager<IdPUser> Users, [FromServices] AccountEmails Emails);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireUser();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await AccountModels.CurrentAsync(request.HttpContext, request.Users);
        if (user == null)
        {
            return Results.Unauthorized();
        }

        if (user.EmailConfirmed || user.Email == null)
        {
            return ApiErrors.Validation("There is no e-mail to verify.");
        }

        await request.Emails.SendVerificationAsync(request.HttpContext, user);
        return Results.NoContent();
    }
}

public sealed record ChangePassword() : PostHandlerAsync<ChangePassword.Request>("/api/account/password")
{
    public sealed record Body(string? CurrentPassword, string NewPassword);

    public readonly record struct Request(HttpContext HttpContext, [FromBody] Body Body, [FromServices] UserManager<IdPUser> Users, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireUser();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await AccountModels.CurrentAsync(request.HttpContext, request.Users);
        if (user == null)
        {
            return Results.Unauthorized();
        }

        IdentityResult result;
        if (user.PasswordHash == null)
        {
            result = await request.Users.AddPasswordAsync(user, request.Body.NewPassword);
        }
        else
        {
            if (string.IsNullOrEmpty(request.Body.CurrentPassword))
            {
                return ApiErrors.Validation("Type your current password.", "currentPassword");
            }

            result = await request.Users.ChangePasswordAsync(user, request.Body.CurrentPassword, request.Body.NewPassword);
        }

        if (!result.Succeeded)
        {
            return ApiErrors.Identity(result);
        }

        await request.Events.RaiseAsync(new AuditEvent("Password changed", user.Id, user.UserName, user.Id, "By the user"));
        return Results.NoContent();
    }
}

public static class Sessions
{
    /// <summary>Ends every session of the user: the cookies stop validating, and the refresh tokens and consents are removed.</summary>
    public static async Task EndAllAsync(IdPUser user, UserManager<IdPUser> users, IPersistedGrantStore grants)
    {
        await users.UpdateSecurityStampAsync(user);
        await grants.RemoveAllAsync(new PersistedGrantFilter { SubjectId = user.Id });
    }
}

public sealed record SetupTotp() : PostHandlerAsync<SetupTotp.Request>("/api/account/totp/setup")
{
    public readonly record struct Request(HttpContext HttpContext, [FromServices] UserManager<IdPUser> Users, [FromServices] IOptions<IdPOptions> Options);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireUser();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await AccountModels.CurrentAsync(request.HttpContext, request.Users);
        if (user == null)
        {
            return Results.Unauthorized();
        }

        await request.Users.ResetAuthenticatorKeyAsync(user);
        var key = await request.Users.GetAuthenticatorKeyAsync(user) ?? throw new InvalidOperationException("No authenticator key.");
        var issuer = request.Options.Value.Name;
        var uri = $"otpauth://totp/{UrlEncoder.Default.Encode(issuer)}:{UrlEncoder.Default.Encode(user.Email ?? user.UserName)}?secret={key}&issuer={UrlEncoder.Default.Encode(issuer)}&digits=6";
        return Results.Ok(new { sharedKey = Format(key), authenticatorUri = uri });
    }

    private static string Format(string key)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < key.Length; i += 4)
        {
            builder.Append(key.AsSpan(i, Math.Min(4, key.Length - i))).Append(' ');
        }

        return builder.ToString().TrimEnd().ToLowerInvariant();
    }
}

public sealed record EnableTotp() : PostHandlerAsync<EnableTotp.Request>("/api/account/totp/enable")
{
    public sealed record Body(string Code);

    public readonly record struct Request(HttpContext HttpContext, [FromBody] Body Body, [FromServices] UserManager<IdPUser> Users, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireUser();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await AccountModels.CurrentAsync(request.HttpContext, request.Users);
        if (user == null)
        {
            return Results.Unauthorized();
        }

        var code = request.Body.Code.Replace(" ", string.Empty, StringComparison.Ordinal);
        if (!await request.Users.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, code))
        {
            return ApiErrors.Validation("That code is not valid. Codes change every 30 seconds.", "code");
        }

        await request.Users.SetTwoFactorEnabledAsync(user, true);
        var codes = await request.Users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        await request.Events.RaiseAsync(new AuditEvent("Authenticator enabled", user.Id, user.UserName, user.Id, "TOTP"));
        return Results.Ok(new { recoveryCodes = codes });
    }
}

public sealed record DisableTotp() : DeleteHandlerAsync<DisableTotp.Request>("/api/account/totp")
{
    public readonly record struct Request(HttpContext HttpContext, [FromServices] UserManager<IdPUser> Users, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireUser();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await AccountModels.CurrentAsync(request.HttpContext, request.Users);
        if (user == null)
        {
            return Results.Unauthorized();
        }

        await request.Users.SetTwoFactorEnabledAsync(user, false);
        user.AuthenticatorKey = null;
        user.RecoveryCodes.Clear();
        await request.Users.UpdateAsync(user);
        await request.Events.RaiseAsync(new AuditEvent("Authenticator disabled", user.Id, user.UserName, user.Id, "TOTP"));
        return Results.NoContent();
    }
}

public sealed record NewRecoveryCodes() : PostHandlerAsync<NewRecoveryCodes.Request>("/api/account/recovery-codes")
{
    public readonly record struct Request(HttpContext HttpContext, [FromServices] UserManager<IdPUser> Users);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireUser();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await AccountModels.CurrentAsync(request.HttpContext, request.Users);
        if (user == null)
        {
            return Results.Unauthorized();
        }

        if (!user.TwoFactorEnabled)
        {
            return ApiErrors.Validation("Recovery codes need an authenticator app.");
        }

        return Results.Ok(new { recoveryCodes = await request.Users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10) });
    }
}

/// <summary>Options to create a passkey for the signed-in user. The ceremony state travels in an encrypted cookie.</summary>
public sealed record PasskeyCreationOptions() : PostHandlerAsync<PasskeyCreationOptions.Request>("/api/account/passkeys/options")
{
    public readonly record struct Request(HttpContext HttpContext, [FromServices] IPasskeyHandler<IdPUser> Passkeys, [FromServices] UserManager<IdPUser> Users, [FromServices] PasskeyState State);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireUser();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await AccountModels.CurrentAsync(request.HttpContext, request.Users);
        if (user == null)
        {
            return Results.Unauthorized();
        }

        var options = await request.Passkeys.MakeCreationOptionsAsync(
            new PasskeyUserEntity { Id = user.Id, Name = user.UserName, DisplayName = user.DisplayName },
            request.HttpContext);
        request.State.Save(request.HttpContext, PasskeyState.Attestation, options.AttestationState ?? throw new InvalidOperationException("No attestation state."));
        return Results.Content(options.CreationOptionsJson, "application/json");
    }
}

public sealed record RegisterPasskey() : PostHandlerAsync<RegisterPasskey.Request>("/api/account/passkeys")
{
    public sealed record Body(string Credential, string? Name);

    public readonly record struct Request(HttpContext HttpContext, [FromBody] Body Body, [FromServices] IPasskeyHandler<IdPUser> Passkeys,
        [FromServices] UserManager<IdPUser> Users, [FromServices] PasskeyState State, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireUser();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var http = request.HttpContext;
        var user = await AccountModels.CurrentAsync(http, request.Users);
        if (user == null)
        {
            return Results.Unauthorized();
        }

        var state = request.State.Take(http, PasskeyState.Attestation);
        if (state == null)
        {
            return ApiErrors.Validation("The passkey registration expired. Try again.");
        }

        var result = await request.Passkeys.PerformAttestationAsync(new PasskeyAttestationContext { HttpContext = http, CredentialJson = request.Body.Credential, AttestationState = state });
        if (!result.Succeeded)
        {
            return ApiErrors.Validation($"The passkey was not accepted: {result.Failure.Message}");
        }

        if (result.UserEntity.Id != user.Id)
        {
            return ApiErrors.Validation("The passkey was created for another user.");
        }

        result.Passkey.Name = string.IsNullOrWhiteSpace(request.Body.Name) ? $"Passkey of {result.Passkey.CreatedAt:yyyy-MM-dd HH:mm}" : request.Body.Name.Trim();
        await request.Users.AddOrUpdatePasskeyAsync(user, result.Passkey);
        await request.Events.RaiseAsync(new AuditEvent("Passkey added", user.Id, user.UserName, user.Id, result.Passkey.Name));
        return Results.Ok(AccountModels.Passkey(PasskeyDocument.From(result.Passkey)));
    }
}

public sealed record RenamePasskey() : PutHandlerAsync<RenamePasskey.Request>("/api/account/passkeys/{id}")
{
    public sealed record Body(string Name);

    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Id, [FromBody] Body Body, [FromServices] UserManager<IdPUser> Users);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireUser();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await AccountModels.CurrentAsync(request.HttpContext, request.Users);
        if (user == null)
        {
            return Results.Unauthorized();
        }

        var passkey = user.Passkeys.FirstOrDefault(p => Base64Url.EncodeToString(p.CredentialId) == request.Id);
        if (passkey == null)
        {
            return Results.NotFound();
        }

        passkey.Name = request.Body.Name.Trim();
        await request.Users.UpdateAsync(user);
        return Results.Ok(AccountModels.Passkey(passkey));
    }
}

public sealed record RemovePasskey() : DeleteHandlerAsync<RemovePasskey.Request>("/api/account/passkeys/{id}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Id, [FromServices] UserManager<IdPUser> Users, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireUser();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await AccountModels.CurrentAsync(request.HttpContext, request.Users);
        if (user == null)
        {
            return Results.Unauthorized();
        }

        await request.Users.RemovePasskeyAsync(user, Base64Url.DecodeFromChars(request.Id));
        await request.Events.RaiseAsync(new AuditEvent("Passkey removed", user.Id, user.UserName, user.Id, request.Id));
        return Results.NoContent();
    }
}

/// <summary>The clients the user has given access to: remembered consents and refresh tokens.</summary>
public sealed record GetApplications() : GetHandlerAsync<GetApplications.Request>("/api/account/applications")
{
    public readonly record struct Request(HttpContext HttpContext, [FromServices] IOidcServerInteractionService Interaction, [FromServices] IClientStore Clients);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireUser();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var grants = await request.Interaction.GetAllUserGrantsAsync();
        var items = new List<ApplicationModel>();
        foreach (var grant in grants)
        {
            var client = await request.Clients.FindClientByIdAsync(grant.ClientId);
            items.Add(new ApplicationModel(grant.ClientId, client?.ClientName ?? grant.ClientId, client?.ClientUri, client?.LogoUri, grant.Scopes.ToList(), grant.CreationTime, grant.Expiration, grant.Description));
        }

        return Results.Ok(items.OrderByDescending(a => a.CreationTime));
    }
}

public sealed record RevokeApplication() : DeleteHandlerAsync<RevokeApplication.Request>("/api/account/applications/{clientId}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string ClientId, [FromServices] IOidcServerInteractionService Interaction, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireUser();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        await request.Interaction.RevokeUserConsentAsync(request.ClientId);
        var sub = request.HttpContext.User.GetSubjectId();
        await request.Events.RaiseAsync(new AuditEvent("Application access revoked", sub, request.HttpContext.User.FindFirst(JwtClaimTypes.PreferredUserName)?.Value, sub, request.ClientId, clientId: request.ClientId));
        return Results.NoContent();
    }
}

/// <summary>The external providers the user can link, and the ones already linked.</summary>
public sealed record GetLinkedAccounts() : GetHandlerAsync<GetLinkedAccounts.Request>("/api/account/external")
{
    public readonly record struct Request(HttpContext HttpContext, [FromServices] UserManager<IdPUser> Users, [FromServices] DynamicSchemeManager Providers);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireUser();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await AccountModels.CurrentAsync(request.HttpContext, request.Users);
        if (user == null)
        {
            return Results.Unauthorized();
        }

        var providers = await request.Providers.GetEnabledAsync(cancellationToken);
        return Results.Ok(providers.Select(p =>
        {
            var login = user.Logins.FirstOrDefault(l => l.LoginProvider == p.Alias);
            return new
            {
                alias = p.Alias,
                displayName = p.DisplayName,
                linked = login != null,
                providerKey = login?.ProviderKey,
                linkedAt = login?.LinkedAt,
                linkUrl = ExternalChallenge.Url(p.Alias, "/account/linked", link: true)
            };
        }));
    }
}

public sealed record UnlinkAccount() : DeleteHandlerAsync<UnlinkAccount.Request>("/api/account/external/{alias}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Alias, [FromServices] UserManager<IdPUser> Users, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireUser();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await AccountModels.CurrentAsync(request.HttpContext, request.Users);
        if (user == null)
        {
            return Results.Unauthorized();
        }

        if (user.PasswordHash == null && user.Passkeys.Count == 0 && !user.Logins.Any(l => l.LoginProvider != request.Alias))
        {
            return ApiErrors.Validation("Set a password or add a passkey first, or you could not sign in any more.");
        }

        foreach (var login in user.Logins.Where(l => l.LoginProvider == request.Alias).ToList())
        {
            await request.Users.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
        }

        await request.Events.RaiseAsync(new AuditEvent("External account unlinked", user.Id, user.UserName, user.Id, request.Alias));
        return Results.NoContent();
    }
}

/// <summary>Ends every session of the user, this one included: the cookies stop validating and the refresh tokens go.</summary>
public sealed record SignOutEverywhere() : PostHandlerAsync<SignOutEverywhere.Request>("/api/account/sign-out-everywhere")
{
    public readonly record struct Request(HttpContext HttpContext, [FromServices] UserManager<IdPUser> Users, [FromServices] IPersistedGrantStore Grants, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireUser();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var user = await AccountModels.CurrentAsync(request.HttpContext, request.Users);
        if (user == null)
        {
            return Results.Unauthorized();
        }

        await Sessions.EndAllAsync(user, request.Users, request.Grants);
        await request.Events.RaiseAsync(new AuditEvent("Signed out everywhere", user.Id, user.UserName, user.Id, "By the user"));
        request.HttpContext.Items[SecurityStampValidation.SkipRefresh] = true;
        await request.HttpContext.SignOutAsync();
        return Results.Ok(new { redirect = "/login" });
    }
}

/// <summary>The user's own recent activity.</summary>
public sealed record GetOwnEvents() : GetHandlerAsync<GetOwnEvents.Request>("/api/account/events")
{
    public readonly record struct Request(HttpContext HttpContext, [FromServices] IdPMongoContext Db);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireUser();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var sub = request.HttpContext.User.GetSubjectId();
        var events = await request.Db.Events.Find(e => e.SubjectId == sub).SortByDescending(e => e.TimeStamp).Limit(50).ToListAsync(cancellationToken);
        return Results.Ok(events.Select(e => new { id = e.Id.ToString(), e.Category, e.Name, e.EventType, e.Message, e.TimeStamp, e.RemoteIpAddress, e.ClientId }));
    }
}
